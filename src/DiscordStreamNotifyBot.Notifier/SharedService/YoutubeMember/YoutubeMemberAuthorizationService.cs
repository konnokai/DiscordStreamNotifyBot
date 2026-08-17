using DiscordStreamNotifyBot.DataBase;
using DiscordStreamNotifyBot.SharedService.Google;
using Google;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using System.Net;

namespace DiscordStreamNotifyBot.SharedService.YoutubeMember
{
    internal enum YoutubeMemberAuthorizationStatus
    {
        Ready,
        AuthorizationInvalid,
        TemporaryFailure,
        LocalContractFailure
    }

    internal readonly record struct YoutubeMemberAuthorizationResult(
        YoutubeMemberAuthorizationStatus Status,
        GoogleCredential Credential,
        string EncryptedTokenPayload);

    internal readonly record struct YoutubeMemberTokenSnapshot(
        TokenResponse Token,
        string EncryptedTokenPayload);

    /// <summary>集中既有 Google flow 與 MySQL token store，維持 TokenManager 密文與資料表契約。</summary>
    public sealed class YoutubeMemberAuthorizationService
    {
        private const string YoutubeScope = "https://www.googleapis.com/auth/youtube.force-ssl";
        private static readonly HttpClient RevokeHttpClient = new();
        private readonly GoogleAuthorizationCodeFlow _flow;
        private readonly MainDbService _dbService;
        private readonly GoogleOAuthOperationLock _operationLock;
        private readonly MySqlDataStore _dataStore;

        public YoutubeMemberAuthorizationService(
            MainDbService dbService,
            BotConfig botConfig,
            GoogleOAuthOperationLock operationLock)
        {
            _dbService = dbService;
            _operationLock = operationLock;
            _dataStore = new MySqlDataStore(dbService, botConfig.ProviderTokenEncryptionKey);
            if (string.IsNullOrWhiteSpace(botConfig.GoogleClientId) ||
                string.IsNullOrWhiteSpace(botConfig.GoogleClientSecret))
                return;

            _flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets
                {
                    ClientId = botConfig.GoogleClientId,
                    ClientSecret = botConfig.GoogleClientSecret
                },
                Scopes = [YoutubeScope],
                DataStore = new NonPersistentGoogleDataStore()
            });
        }

        public bool IsConfigured => _flow != null;

        public async Task<bool> IsExistUserTokenAsync(string discordUserId)
            => _flow != null && await _dataStore.IsExistUserTokenAsync<TokenResponse>(discordUserId);

        internal async Task<YoutubeMemberAuthorizationResult> GetCredentialAsync(
            string discordUserId,
            CancellationToken cancellationToken)
        {
            if (_flow == null || string.IsNullOrWhiteSpace(discordUserId))
                return new(YoutubeMemberAuthorizationStatus.LocalContractFailure, null, null);

            string encryptedTokenPayload;
            using (var db = _dbService.GetDbContext())
            {
                encryptedTokenPayload = await db.YoutubeMemberAccessToken.AsNoTracking()
                    .Where(x => x.DiscordUserId == ulong.Parse(discordUserId))
                    .Select(x => x.EncryptedAccessToken)
                    .SingleOrDefaultAsync(cancellationToken);
            }
            if (string.IsNullOrEmpty(encryptedTokenPayload))
                return new(YoutubeMemberAuthorizationStatus.LocalContractFailure, null, null);

            TokenResponse token;
            try
            {
                token = await _dataStore.GetAsync<TokenResponse>(discordUserId);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Log.Warn(YoutubeMemberSafeLogging.DescribeFailure("讀取 YouTube 憑證，保留既有 entitlement", ex));
                return new(YoutubeMemberAuthorizationStatus.LocalContractFailure, null, null);
            }
            if (token == null || string.IsNullOrWhiteSpace(token.RefreshToken))
                return new(YoutubeMemberAuthorizationStatus.LocalContractFailure, null, null);

            var credential = GoogleCredential.FromAccessToken(token.AccessToken);
            if (!token.IsStale)
                return new(YoutubeMemberAuthorizationStatus.Ready, credential, encryptedTokenPayload);

            GoogleOAuthOperationLockAcquireResult lockResult = await _operationLock.TryAcquireAsync(
                ulong.Parse(discordUserId), cancellationToken);
            if (lockResult.Status != GoogleOAuthOperationLockAcquireStatus.Acquired)
            {
                Log.Warn($"YouTube OAuth refresh 無法取得跨程序 lease: {discordUserId} / {lockResult.Status}");
                return new(YoutubeMemberAuthorizationStatus.TemporaryFailure, null, encryptedTokenPayload);
            }
            await using var operationLease = lockResult.Lease;

            try
            {
                encryptedTokenPayload = await GetEncryptedTokenPayloadAsync(discordUserId, cancellationToken);
                if (await _dataStore.HasUnlinkIntentAsync(ulong.Parse(discordUserId), cancellationToken))
                    return new(YoutubeMemberAuthorizationStatus.TemporaryFailure, null, encryptedTokenPayload);
                token = await _dataStore.GetAsync<TokenResponse>(discordUserId);
                if (token == null || string.IsNullOrWhiteSpace(token.RefreshToken) ||
                    string.IsNullOrEmpty(encryptedTokenPayload))
                {
                    return new(YoutubeMemberAuthorizationStatus.LocalContractFailure, null, null);
                }
                credential = GoogleCredential.FromAccessToken(token.AccessToken);
                if (!token.IsStale)
                    return new(YoutubeMemberAuthorizationStatus.Ready, credential, encryptedTokenPayload);
                if (await operationLease.EnsureOwnedAsync(cancellationToken) !=
                    GoogleOAuthOperationLockOwnershipStatus.Owned)
                {
                    return new(YoutubeMemberAuthorizationStatus.TemporaryFailure, null, encryptedTokenPayload);
                }
                string expectedEncryptedToken = encryptedTokenPayload;
                TokenResponse refreshedToken = await _flow.RefreshTokenAsync(
                    discordUserId, token.RefreshToken, cancellationToken);
                refreshedToken.RefreshToken ??= token.RefreshToken;
                if (await operationLease.EnsureOwnedAsync(cancellationToken) !=
                        GoogleOAuthOperationLockOwnershipStatus.Owned ||
                    !await _dataStore.StoreRefreshIfCurrentAsync(
                        ulong.Parse(discordUserId),
                        expectedEncryptedToken,
                        refreshedToken,
                        cancellationToken))
                {
                    return new(YoutubeMemberAuthorizationStatus.TemporaryFailure, null, expectedEncryptedToken);
                }
                string refreshedPayload = await GetEncryptedTokenPayloadAsync(discordUserId, cancellationToken);
                return string.IsNullOrEmpty(refreshedPayload)
                    ? new(YoutubeMemberAuthorizationStatus.LocalContractFailure, null, null)
                    : new(
                        YoutubeMemberAuthorizationStatus.Ready,
                        GoogleCredential.FromAccessToken(refreshedToken.AccessToken),
                        refreshedPayload);
            }
            catch (GoogleApiException exception) when (YoutubeMemberApiClient.IsConclusiveAuthorizationInvalidation(
                (int)exception.HttpStatusCode, exception.Error?.Errors?.Select(error => error.Reason)))
            {
                // cleanup orchestrator 會先 durable 地標記所有 check，再移除本機 token，不能在此留下無 token 的 active check。
                return new(YoutubeMemberAuthorizationStatus.AuthorizationInvalid, null, encryptedTokenPayload);
            }
            catch (TokenResponseException exception) when (IsInvalidGrant(exception))
            {
                return new(YoutubeMemberAuthorizationStatus.AuthorizationInvalid, null, encryptedTokenPayload);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                Log.Warn(YoutubeMemberSafeLogging.DescribeFailure("刷新 YouTube 憑證暫時失敗，保留既有 entitlement", ex));
                return new(YoutubeMemberAuthorizationStatus.TemporaryFailure, null, encryptedTokenPayload);
            }
        }

        public async Task<string> GetLinkedChannelAsync(string discordUserId, CancellationToken cancellationToken)
        {
            YoutubeMemberAuthorizationResult authorization = await GetCredentialAsync(discordUserId, cancellationToken);
            if (authorization.Status != YoutubeMemberAuthorizationStatus.Ready)
                throw new InvalidOperationException("Google 憑證目前無法使用。");

            var request = new YouTubeService(new BaseClientService.Initializer
            {
                HttpClientInitializer = authorization.Credential,
                ApplicationName = "Discord Youtube Member Check"
            }).Channels.List("id,snippet");
            request.Mine = true;
            var channel = (await request.ExecuteAsync(cancellationToken)).Items.FirstOrDefault();
            if (channel == null)
                throw new InvalidOperationException("找不到已連結的 YouTube 頻道。");
            return Format.Url(channel.Snippet.Title, $"https://www.youtube.com/channel/{channel.Id}");
        }

        internal async Task<YoutubeMemberTokenSnapshot?> GetTokenSnapshotAsync(
            string discordUserId,
            CancellationToken cancellationToken)
        {
            if (_flow == null || string.IsNullOrWhiteSpace(discordUserId))
                throw new InvalidOperationException("Google OAuth 尚未設定。");
            string encryptedTokenPayload;
            using (var db = _dbService.GetDbContext())
            {
                encryptedTokenPayload = await db.YoutubeMemberAccessToken.AsNoTracking()
                    .Where(x => x.DiscordUserId == ulong.Parse(discordUserId))
                    .Select(x => x.EncryptedAccessToken)
                    .SingleOrDefaultAsync(cancellationToken);
            }
            if (string.IsNullOrEmpty(encryptedTokenPayload))
                return null;
            TokenResponse token = await _dataStore.GetAsync<TokenResponse>(discordUserId);
            if (token == null)
                return null;
            return new(token, encryptedTokenPayload);
        }

        private async Task<string> GetEncryptedTokenPayloadAsync(string discordUserId, CancellationToken cancellationToken)
        {
            using var db = _dbService.GetDbContext();
            return await db.YoutubeMemberAccessToken.AsNoTracking()
                .Where(x => x.DiscordUserId == ulong.Parse(discordUserId))
                .Select(x => x.EncryptedAccessToken)
                .SingleOrDefaultAsync(cancellationToken);
        }

        /// <summary>
        /// 直接呼叫 provider，讓呼叫端先保存 cleanup intent，再明確刪除本機密文。
        /// 只有成功或明確 invalid_token 代表已撤銷；網路、5xx 與其他 4xx 均保留 token 與 fence。
        /// </summary>
        internal async Task RevokeAsync(YoutubeMemberTokenSnapshot snapshot, CancellationToken cancellationToken)
        {
            TokenResponse token = snapshot.Token;
            string revokeToken = token.RefreshToken ?? token.AccessToken;
            if (string.IsNullOrWhiteSpace(revokeToken))
                throw new InvalidOperationException("Google 憑證資料不完整。");
            using var content = new FormUrlEncodedContent([new KeyValuePair<string, string>("token", revokeToken)]);
            using HttpResponseMessage response = await RevokeHttpClient.PostAsync(
                "https://oauth2.googleapis.com/revoke", content, cancellationToken);
            if (response.IsSuccessStatusCode)
                return;
            string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!IsConclusiveAlreadyRevoked((int)response.StatusCode, responseBody))
                response.EnsureSuccessStatusCode();
        }

        internal static bool IsConclusiveAlreadyRevoked(int statusCode, string responseBody)
        {
            if (statusCode < (int)HttpStatusCode.BadRequest || statusCode >= 500 ||
                string.IsNullOrWhiteSpace(responseBody))
            {
                return false;
            }

            try
            {
                return string.Equals(
                    Newtonsoft.Json.Linq.JObject.Parse(responseBody).Value<string>("error"),
                    "invalid_token",
                    StringComparison.OrdinalIgnoreCase);
            }
            catch (JsonException)
            {
                return false;
            }
        }

        internal static bool IsInvalidGrant(TokenResponseException exception)
            => string.Equals(exception?.Error?.Error, "invalid_grant", StringComparison.OrdinalIgnoreCase);
    }
}
