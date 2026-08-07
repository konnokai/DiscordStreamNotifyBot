using Google;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using System.Net;

namespace DiscordStreamNotifyBot.SharedService.YoutubeMember
{
    public enum YoutubeMemberProbeResultKind
    {
        Member,
        NotMember,
        AuthorizationInvalid,
        ProbeVideoInvalid,
        QuotaExceeded,
        RateLimited,
        TemporaryFailure,
        LocalContractFailure
    }

    internal readonly record struct YoutubeMemberProbeResult(YoutubeMemberProbeResultKind Kind)
    {
        public bool PreservesEntitlement => Kind is not YoutubeMemberProbeResultKind.NotMember and
            not YoutubeMemberProbeResultKind.AuthorizationInvalid;
    }

    /// <summary>封裝會員限定影片留言探測，避免呼叫端依賴 Google SDK 的例外訊息文字。</summary>
    public sealed class YoutubeMemberApiClient
    {
        private static readonly HashSet<string> QuotaReasons = new(StringComparer.OrdinalIgnoreCase)
        {
            "quotaExceeded", "dailyLimitExceeded", "dailyLimitExceededUnreg"
        };
        private static readonly HashSet<string> RateLimitReasons = new(StringComparer.OrdinalIgnoreCase)
        {
            "rateLimitExceeded", "userRateLimitExceeded"
        };
        private static readonly HashSet<string> AuthorizationReasons = new(StringComparer.OrdinalIgnoreCase)
        {
            "authError", "invalidCredentials", "invalid_grant", "tokenExpired",
            "accountDisabled", "accountSuspended"
        };
        private static readonly HashSet<string> InvalidVideoReasons = new(StringComparer.OrdinalIgnoreCase)
        {
            "commentsDisabled", "videoNotFound", "invalidVideoId", "notFound"
        };
        // 只有已由 channels.mine 證實憑證與 scope 正常後，留言 probe 的 documented forbidden 才能表示非會員。
        private static readonly HashSet<string> NotMemberReasons = new(StringComparer.OrdinalIgnoreCase)
        {
            "forbidden"
        };

        /// <summary>以最小 authenticated request 驗證 credential 與 scope；每個 user/cycle 只可呼叫一次。</summary>
        internal async Task<YoutubeMemberProbeResult> ValidateAuthorizationAsync(
            GoogleCredential credential,
            CancellationToken cancellationToken)
        {
            if (credential == null)
                return new(YoutubeMemberProbeResultKind.LocalContractFailure);

            try
            {
                var request = new YouTubeService(new BaseClientService.Initializer
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "Discord Youtube Member Check"
                }).Channels.List("id");
                request.Mine = true;
                await request.ExecuteAsync(cancellationToken).ConfigureAwait(false);
                return new(YoutubeMemberProbeResultKind.Member);
            }
            catch (GoogleApiException exception)
            {
                return new(Classify((int)exception.HttpStatusCode,
                    exception.Error?.Errors?.Select(error => error.Reason)));
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (OperationCanceledException)
            {
                return new(YoutubeMemberProbeResultKind.TemporaryFailure);
            }
            catch (HttpRequestException)
            {
                return new(YoutubeMemberProbeResultKind.TemporaryFailure);
            }
            catch
            {
                return new(YoutubeMemberProbeResultKind.LocalContractFailure);
            }
        }

        internal async Task<YoutubeMemberProbeResult> ProbeAsync(
            GoogleCredential credential,
            string videoId,
            bool authorizationValidated,
            CancellationToken cancellationToken)
        {
            if (credential == null || string.IsNullOrWhiteSpace(videoId) || videoId == "-")
                return new(YoutubeMemberProbeResultKind.LocalContractFailure);

            try
            {
                var request = new YouTubeService(new BaseClientService.Initializer
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "Discord Youtube Member Check"
                }).CommentThreads.List("id");
                request.VideoId = videoId;
                await request.ExecuteAsync(cancellationToken).ConfigureAwait(false);
                return new(YoutubeMemberProbeResultKind.Member);
            }
            catch (GoogleApiException exception)
            {
                return new(Classify((int)exception.HttpStatusCode,
                    exception.Error?.Errors?.Select(error => error.Reason), authorizationValidated));
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (OperationCanceledException)
            {
                return new(YoutubeMemberProbeResultKind.TemporaryFailure);
            }
            catch (HttpRequestException)
            {
                return new(YoutubeMemberProbeResultKind.TemporaryFailure);
            }
            catch
            {
                // SDK 組態、序列化等本機契約問題不可破壞既有 entitlement。
                return new(YoutubeMemberProbeResultKind.LocalContractFailure);
            }
        }

        internal static YoutubeMemberProbeResultKind Classify(
            int? statusCode,
            IEnumerable<string> reasons,
            bool authorizationValidated = false)
        {
            var reasonSet = (reasons ?? []).Where(reason => !string.IsNullOrWhiteSpace(reason))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (reasonSet.Overlaps(QuotaReasons))
                return YoutubeMemberProbeResultKind.QuotaExceeded;
            if (statusCode == (int)HttpStatusCode.TooManyRequests || reasonSet.Overlaps(RateLimitReasons))
                return YoutubeMemberProbeResultKind.RateLimited;
            if (statusCode == (int)HttpStatusCode.Unauthorized || reasonSet.Overlaps(AuthorizationReasons))
                return YoutubeMemberProbeResultKind.AuthorizationInvalid;
            if (statusCode == (int)HttpStatusCode.NotFound || reasonSet.Overlaps(InvalidVideoReasons))
                return YoutubeMemberProbeResultKind.ProbeVideoInvalid;
            if (authorizationValidated && statusCode == (int)HttpStatusCode.Forbidden &&
                reasonSet.Overlaps(NotMemberReasons))
                return YoutubeMemberProbeResultKind.NotMember;
            if (statusCode >= 500 || statusCode == (int)HttpStatusCode.RequestTimeout)
                return YoutubeMemberProbeResultKind.TemporaryFailure;

            // 400/403 與未知 response 都不是會員資格的確定否定。
            return YoutubeMemberProbeResultKind.TemporaryFailure;
        }

        internal static bool IsConclusiveAuthorizationInvalidation(int? statusCode, IEnumerable<string> reasons)
            => Classify(statusCode, reasons) == YoutubeMemberProbeResultKind.AuthorizationInvalid;

        internal static bool IsDocumentedMembershipForbidden(GoogleApiException exception)
            => exception != null && exception.HttpStatusCode == HttpStatusCode.Forbidden &&
                exception.Error?.Errors?.Any(error =>
                    string.Equals(error.Reason, "forbidden", StringComparison.OrdinalIgnoreCase)) == true;

        internal static bool HasReason(GoogleApiException exception, string reason)
            => exception?.Error?.Errors?.Any(error =>
                string.Equals(error.Reason, reason, StringComparison.OrdinalIgnoreCase)) == true;
    }
}
