using DiscordStreamNotifyBot.Shared;
using DiscordStreamNotifyBot.SharedService.Youtube;
using StackExchange.Redis;
using System.Net;

namespace DiscordStreamNotifyBot.Tests.Component.Redis
{
    /// <summary>
    /// WebSub 共享狀態的 Redis component 測試（計畫 §7.1）。使用獨立的 component 資料庫，
    /// 驗證 pending 往返、secret 只建立一次，以及「重試可延長但不得縮短 TTL」。
    /// </summary>
    [Collection(RedisComponentCollection.Name)]
    [Trait("Category", "RedisComponent")]
    public sealed class YoutubeWebSubStateRedisComponentTests
    {
        private const string ChannelId = "UCabcdefghijklmnopqrstuv";

        private readonly RedisComponentFixture _fixture;

        public YoutubeWebSubStateRedisComponentTests(RedisComponentFixture fixture)
        {
            _fixture = fixture;
        }

        [RedisComponentFact]
        public async Task PendingActionRoundTripsWithRequestedLeaseTtl()
        {
            IDatabase db = _fixture.Database;
            RedisKey key = RedisChannels.YoutubeWebSub.PendingKey(ChannelId);
            await RedisComponentFixture.AssertKeysAbsentAsync(db, key);
            var state = new YoutubeWebSubState(db);

            try
            {
                Assert.Null(await state.GetPendingAsync(ChannelId));

                var action = YoutubeWebSubPendingAction.Create(
                    ChannelId, YoutubeWebSubContract.ModeSubscribe, "token-value", DateTime.UtcNow);
                Assert.True(await state.TryWritePendingAsync(null, action, YoutubeWebSubContract.PendingTtl));

                var loaded = await state.GetPendingAsync(ChannelId);
                Assert.NotNull(loaded);
                Assert.Equal(action.ChannelId, loaded.ChannelId);
                Assert.Equal(action.Mode, loaded.Mode);
                Assert.Equal(action.Topic, loaded.Topic);
                Assert.Equal(action.CallbackToken, loaded.CallbackToken);
                Assert.Equal(action.RequestedAtUtc, loaded.RequestedAtUtc);

                TimeSpan? ttl = await state.GetPendingTtlAsync(ChannelId);
                Assert.NotNull(ttl);
                Assert.InRange(ttl.Value, TimeSpan.FromDays(10) - TimeSpan.FromMinutes(1), TimeSpan.FromDays(10));

                // CAS：用過期快照寫入必須失敗，不會覆蓋另一側剛寫入的狀態。
                var staleSnapshot = await state.GetPendingSnapshotAsync(ChannelId);
                var updated = YoutubeWebSubPendingAction.Create(
                    ChannelId, YoutubeWebSubContract.ModeSubscribe, "token-value", DateTime.UtcNow.AddSeconds(5));
                updated.ConfirmedAtUtc = DateTime.UtcNow;
                Assert.True(await state.TryWritePendingAsync(staleSnapshot, updated, YoutubeWebSubContract.PendingTtl));

                Assert.False(await state.TryWritePendingAsync(staleSnapshot, staleSnapshot.Action, YoutubeWebSubContract.PendingTtl));
                Assert.NotNull((await state.GetPendingAsync(ChannelId)).ConfirmedAtUtc);
            }
            finally
            {
                await db.KeyDeleteAsync(key);
            }
        }

        [RedisComponentFact]
        public async Task SecretIsCreatedOnceAndExistingTtlIsNotExtendedBeforeChallenge()
        {
            IDatabase db = _fixture.Database;
            RedisKey key = RedisChannels.YoutubeWebSub.HmacSecretKey(ChannelId);
            await RedisComponentFixture.AssertKeysAbsentAsync(db, key);
            var state = new YoutubeWebSubState(db);

            try
            {
                string first = await state.GetOrCreateSecretAsync(ChannelId, TimeSpan.FromHours(1));
                string second = await state.GetOrCreateSecretAsync(ChannelId, YoutubeWebSubContract.PendingTtl);

                // 既有 secret 不得因為再次送出要求而被延長 TTL，否則失敗的提早續訂會被續訂篩選略過。
                Assert.Equal(first, second);
                TimeSpan? ttl = await state.GetSecretTtlAsync(ChannelId);
                Assert.NotNull(ttl);
                Assert.InRange(ttl.Value, TimeSpan.FromMinutes(59), TimeSpan.FromHours(1));
            }
            finally
            {
                await db.KeyDeleteAsync(key);
            }
        }

        [RedisComponentFact]
        public async Task FailedSendKeepsRenewalDueUntilChallengeConfirms()
        {
            IDatabase db = _fixture.Database;
            RedisKey pendingKey = RedisChannels.YoutubeWebSub.PendingKey(ChannelId);
            RedisKey secretKey = RedisChannels.YoutubeWebSub.HmacSecretKey(ChannelId);
            RedisKey lockKey = RedisChannels.YoutubeWebSub.InFlightKey(ChannelId);
            await RedisComponentFixture.AssertKeysAbsentAsync(db, pendingKey, secretKey, lockKey);
            var handler = new CountingHandler(HttpStatusCode.ServiceUnavailable);
            var state = new YoutubeWebSubState(db);
            var service = new YoutubeWebSubService(
                new BotConfig { ApiServerDomain = "api.example.com" },
                new StubHttpClientFactory(handler),
                state);

            try
            {
                DateTime now = DateTime.Now;

                // 剛成功訂閱過（LastSubscribeTime = now），但 Hub 回 5xx：secret TTL 已被 Backend 設成短 lease，
                // 且 pending 仍未確認，因此下一輪必須再試。
                Assert.True(await state.TryWritePendingAsync(
                    null,
                    YoutubeWebSubPendingAction.Create(ChannelId, YoutubeWebSubContract.ModeSubscribe, "old-token", DateTime.UtcNow),
                    YoutubeWebSubContract.PendingTtl));

                YoutubeWebSubRequestResult result = await service.RequestAsync(ChannelId, subscribe: true, renewDue: true);
                Assert.Equal(YoutubeWebSubRequestOutcome.TransientFailure, result.Outcome);

                YoutubeWebSubPendingAction pending = await state.GetPendingAsync(ChannelId);
                Assert.NotNull(pending);
                Assert.Null(pending.ConfirmedAtUtc);
                Assert.True(await service.IsRenewalDueAsync(ChannelId, now, now));

                // challenge 成功後（confirmed）就不再是 due。
                YoutubeWebSubPendingSnapshot snapshot = await state.GetPendingSnapshotAsync(ChannelId);
                Assert.True(await state.TryWritePendingAsync(snapshot, new YoutubeWebSubPendingAction
                {
                    ChannelId = snapshot.Action.ChannelId,
                    Mode = snapshot.Action.Mode,
                    Topic = snapshot.Action.Topic,
                    CallbackToken = snapshot.Action.CallbackToken,
                    RequestedAtUtc = snapshot.Action.RequestedAtUtc,
                    ConfirmedAtUtc = DateTime.UtcNow,
                }, YoutubeWebSubContract.PendingTtl));

                Assert.False(await service.IsRenewalDueAsync(ChannelId, now, now));
            }
            finally
            {
                await db.KeyDeleteAsync(pendingKey);
                await db.KeyDeleteAsync(secretKey);
                await db.KeyDeleteAsync(lockKey);
            }
        }

        [RedisComponentFact]
        public async Task UnparsablePendingIsReplacedByNextRequest()
        {
            IDatabase db = _fixture.Database;
            RedisKey key = RedisChannels.YoutubeWebSub.PendingKey(ChannelId);
            RedisKey secretKey = RedisChannels.YoutubeWebSub.HmacSecretKey(ChannelId);
            RedisKey lockKey = RedisChannels.YoutubeWebSub.InFlightKey(ChannelId);
            await RedisComponentFixture.AssertKeysAbsentAsync(db, key, secretKey, lockKey);
            var state = new YoutubeWebSubState(db);
            var service = new YoutubeWebSubService(
                new BotConfig { ApiServerDomain = "api.example.com" },
                new StubHttpClientFactory(new CountingHandler()),
                state);

            try
            {
                await db.StringSetAsync(key, "{ not a pending action }");

                // 損壞的內容視為沒有 pending，但要留下原始值才能被取代。
                YoutubeWebSubPendingSnapshot snapshot = await state.GetPendingSnapshotAsync(ChannelId);
                Assert.NotNull(snapshot);
                Assert.Null(snapshot.Action);
                Assert.Null(await state.GetPendingAsync(ChannelId));

                YoutubeWebSubRequestResult result = await service.RequestAsync(ChannelId, subscribe: true, renewDue: true);
                Assert.Equal(YoutubeWebSubRequestOutcome.Accepted, result.Outcome);

                YoutubeWebSubPendingAction replaced = await state.GetPendingAsync(ChannelId);
                Assert.NotNull(replaced);
                Assert.Equal(YoutubeWebSubContract.ModeSubscribe, replaced.Mode);
            }
            finally
            {
                await db.KeyDeleteAsync(key);
                await db.KeyDeleteAsync(secretKey);
                await db.KeyDeleteAsync(lockKey);
            }
        }

        [RedisComponentFact]
        public void AtomValidatorStoreUsesDatabaseZero()
        {
            // 只檢查 logical database 的選擇，不對 production DB 0 做任何讀寫。
            Assert.Equal(0, YoutubeAtomValidatorStore.Create(_fixture.Connection).DatabaseNumber);
            Assert.Equal(0, RedisChannels.OAuth.DatabaseNumber - 1);
        }

        [RedisComponentFact]
        public async Task ConcurrentIdenticalRequestsSendOnlyOneHubRequest()
        {
            IDatabase db = _fixture.Database;
            RedisKey pendingKey = RedisChannels.YoutubeWebSub.PendingKey(ChannelId);
            RedisKey secretKey = RedisChannels.YoutubeWebSub.HmacSecretKey(ChannelId);
            await RedisComponentFixture.AssertKeysAbsentAsync(db, pendingKey, secretKey);
            var handler = new CountingHandler();
            var service = new YoutubeWebSubService(
                new BotConfig { ApiServerDomain = "api.example.com" },
                new StubHttpClientFactory(handler),
                new YoutubeWebSubState(db));

            try
            {
                Task<YoutubeWebSubRequestResult>[] calls = Enumerable.Range(0, 5)
                    .Select(_ => service.RequestAsync(ChannelId, subscribe: true, renewDue: true))
                    .ToArray();
                YoutubeWebSubRequestResult[] results = await Task.WhenAll(calls);

                Assert.All(results, result => Assert.Equal(YoutubeWebSubRequestOutcome.Accepted, result.Outcome));
                Assert.Equal(1, handler.RequestCount);

                string form = handler.LastFormBody;
                Assert.Contains($"hub.topic=https%3A%2F%2Fwww.youtube.com%2Ffeeds%2Fvideos.xml%3Fchannel_id%3D{ChannelId}", form, StringComparison.Ordinal);
                Assert.Contains($"hub.callback=https%3A%2F%2Fapi.example.com%2FNotificationCallback%3FchannelId%3D{ChannelId}%26token%3D", form, StringComparison.Ordinal);
                Assert.Contains("hub.lease_seconds=864000", form, StringComparison.Ordinal);
                Assert.DoesNotContain("hub.verify", form, StringComparison.Ordinal);
                Assert.DoesNotContain("/xml/feeds/", form, StringComparison.Ordinal);

                // secret 只建立一次，且 callback URL 內是 secret 的衍生 token，不是 secret 本身。
                string secret = await new YoutubeWebSubState(db).GetSecretAsync(ChannelId);
                Assert.NotNull(secret);
                Assert.Contains($"token%3D{YoutubeWebSubContract.DeriveCallbackToken(secret)}", form, StringComparison.Ordinal);
                Assert.DoesNotContain($"token%3D{secret}", form, StringComparison.Ordinal);
            }
            finally
            {
                await db.KeyDeleteAsync(pendingKey);
                await db.KeyDeleteAsync(secretKey);
            }
        }

        [RedisComponentFact]
        public async Task DeniedPendingIsNotResentUntilForced()
        {
            IDatabase db = _fixture.Database;
            RedisKey pendingKey = RedisChannels.YoutubeWebSub.PendingKey(ChannelId);
            RedisKey secretKey = RedisChannels.YoutubeWebSub.HmacSecretKey(ChannelId);
            await RedisComponentFixture.AssertKeysAbsentAsync(db, pendingKey, secretKey);
            var handler = new CountingHandler();
            var service = new YoutubeWebSubService(
                new BotConfig { ApiServerDomain = "api.example.com" },
                new StubHttpClientFactory(handler),
                new YoutubeWebSubState(db));

            try
            {
                var denied = YoutubeWebSubPendingAction.Create(
                    ChannelId, YoutubeWebSubContract.ModeSubscribe, "token-value", DateTime.UtcNow);
                denied.DeniedAtUtc = DateTime.UtcNow;
                Assert.True(await new YoutubeWebSubState(db).TryWritePendingAsync(null, denied, YoutubeWebSubContract.PendingTtl));

                YoutubeWebSubRequestResult blocked = await service.RequestAsync(ChannelId, subscribe: true, renewDue: true);
                Assert.Equal(YoutubeWebSubRequestOutcome.Suppressed, blocked.Outcome);
                Assert.Equal(0, handler.RequestCount);
                Assert.Null(await new YoutubeWebSubState(db).GetSecretAsync(ChannelId));

                // owner 強制重新訂閱可清除 denied 並建立新 pending。
                YoutubeWebSubRequestResult forced = await service.RequestAsync(ChannelId, subscribe: true, force: true, renewDue: true);
                Assert.Equal(YoutubeWebSubRequestOutcome.Accepted, forced.Outcome);
                Assert.Equal(1, handler.RequestCount);

                YoutubeWebSubPendingAction updated = await new YoutubeWebSubState(db).GetPendingAsync(ChannelId);
                Assert.Null(updated.DeniedAtUtc);
                Assert.Null(updated.ConfirmedAtUtc);
            }
            finally
            {
                await db.KeyDeleteAsync(pendingKey);
                await db.KeyDeleteAsync(secretKey);
            }
        }

        private sealed class CountingHandler(HttpStatusCode statusCode = HttpStatusCode.Accepted) : HttpMessageHandler
        {
            public int RequestCount { get; private set; }

            public string LastFormBody { get; private set; } = "";

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                RequestCount++;
                LastFormBody = request.Content == null ? "" : await request.Content.ReadAsStringAsync(cancellationToken);

                // 讓併發呼叫有機會重疊，驗證相同要求只送一次。
                await Task.Delay(50, cancellationToken);
                return new HttpResponseMessage(statusCode);
            }
        }

        private sealed class StubHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
        {
            public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
        }
    }
}
