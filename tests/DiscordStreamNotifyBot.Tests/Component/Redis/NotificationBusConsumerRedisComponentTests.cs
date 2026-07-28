using DiscordStreamNotifyBot.Shared;
using DiscordStreamNotifyBot.Shared.Messages;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace DiscordStreamNotifyBot.Tests.Component.Redis
{
    [Collection(RedisComponentCollection.Name)]
    [Trait("Category", "RedisComponent")]
    public sealed class NotificationBusConsumerRedisComponentTests
    {
        private readonly RedisComponentFixture _fixture;

        public NotificationBusConsumerRedisComponentTests(RedisComponentFixture fixture)
        {
            _fixture = fixture;
        }

        [RedisComponentFact]
        public async Task SuccessfulDispatchWritesDedupMarkerAndAcknowledgesEntry()
        {
            var db = _fixture.Database;
            const int shardId = 9201;
            var dto = CreateNotification();
            string payload = JsonConvert.SerializeObject(dto);
            string dedupKey = NotificationDedupPolicy.TryGetKey(shardId, NotifyType.Youtube, payload);
            await RedisComponentFixture.AssertKeysAbsentAsync(db, NotificationBus.StreamKey, dedupKey);
            int dispatchCount = 0;

            try
            {
                var entry = await CreatePendingEntryAsync(db, shardId, dto);
                var consumer = new NotificationBusConsumer((type, json) =>
                {
                    Assert.Equal(NotifyType.Youtube, type);
                    Assert.Equal(payload, json);
                    Interlocked.Increment(ref dispatchCount);
                    return Task.CompletedTask;
                });

                await consumer.ProcessEntryAsync(db, shardId, entry);

                Assert.Equal(1, dispatchCount);
                Assert.True(await db.KeyExistsAsync(dedupKey));
                Assert.True((await db.KeyTimeToLiveAsync(dedupKey)) > TimeSpan.Zero);
                Assert.Equal(0, (await db.StreamPendingAsync(
                    NotificationBus.StreamKey, NotificationBus.GroupName(shardId))).PendingMessageCount);
            }
            finally
            {
                await db.KeyDeleteAsync([NotificationBus.StreamKey, dedupKey]);
            }
        }

        [RedisComponentFact]
        public async Task DispatchFailureLeavesEntryPendingWithoutDedupMarker()
        {
            var db = _fixture.Database;
            const int shardId = 9202;
            var dto = CreateNotification();
            string payload = JsonConvert.SerializeObject(dto);
            string dedupKey = NotificationDedupPolicy.TryGetKey(shardId, NotifyType.Youtube, payload);
            await RedisComponentFixture.AssertKeysAbsentAsync(db, NotificationBus.StreamKey, dedupKey);

            try
            {
                var entry = await CreatePendingEntryAsync(db, shardId, dto);
                var consumer = new NotificationBusConsumer((_, _) =>
                    Task.FromException(new InvalidOperationException("component dispatch failure")));

                await consumer.ProcessEntryAsync(db, shardId, entry);

                Assert.False(await db.KeyExistsAsync(dedupKey));
                Assert.Equal(1, (await db.StreamPendingAsync(
                    NotificationBus.StreamKey, NotificationBus.GroupName(shardId))).PendingMessageCount);
            }
            finally
            {
                await db.KeyDeleteAsync([NotificationBus.StreamKey, dedupKey]);
            }
        }

        [RedisComponentFact]
        public async Task ExistingMarkerSkipsDispatchAndAcknowledgesEntry()
        {
            var db = _fixture.Database;
            const int shardId = 9203;
            var dto = CreateNotification();
            string payload = JsonConvert.SerializeObject(dto);
            string dedupKey = NotificationDedupPolicy.TryGetKey(shardId, NotifyType.Youtube, payload);
            await RedisComponentFixture.AssertKeysAbsentAsync(db, NotificationBus.StreamKey, dedupKey);
            int dispatchCount = 0;

            try
            {
                var entry = await CreatePendingEntryAsync(db, shardId, dto);
                await db.StringSetAsync(dedupKey, "1", TimeSpan.FromMinutes(5));
                var consumer = new NotificationBusConsumer((_, _) =>
                {
                    Interlocked.Increment(ref dispatchCount);
                    return Task.CompletedTask;
                });

                await consumer.ProcessEntryAsync(db, shardId, entry);

                Assert.Equal(0, dispatchCount);
                Assert.Equal(0, (await db.StreamPendingAsync(
                    NotificationBus.StreamKey, NotificationBus.GroupName(shardId))).PendingMessageCount);
            }
            finally
            {
                await db.KeyDeleteAsync([NotificationBus.StreamKey, dedupKey]);
            }
        }

        [RedisComponentFact]
        public async Task ReclaimedEntryWithMarkerDoesNotDispatchAgain()
        {
            var db = _fixture.Database;
            const int shardId = 9204;
            var dto = CreateNotification();
            string payload = JsonConvert.SerializeObject(dto);
            string dedupKey = NotificationDedupPolicy.TryGetKey(shardId, NotifyType.Youtube, payload);
            await RedisComponentFixture.AssertKeysAbsentAsync(db, NotificationBus.StreamKey, dedupKey);
            int dispatchCount = 0;

            try
            {
                var entry = await CreatePendingEntryAsync(db, shardId, dto);
                await db.StringSetAsync(dedupKey, "1", TimeSpan.FromMinutes(5));

                using var restartedConnection = await _fixture.OpenConnectionAsync();
                var restartedDb = restartedConnection.GetDatabase();
                var claimed = Assert.Single(await NotificationBus.AutoClaimAsync(
                    restartedDb, shardId, TimeSpan.Zero, 1));
                Assert.Equal(entry.Id, claimed.Id);

                var consumer = new NotificationBusConsumer((_, _) =>
                {
                    Interlocked.Increment(ref dispatchCount);
                    return Task.CompletedTask;
                });
                await consumer.ProcessEntryAsync(restartedDb, shardId, claimed);

                Assert.Equal(0, dispatchCount);
                Assert.Equal(0, (await restartedDb.StreamPendingAsync(
                    NotificationBus.StreamKey, NotificationBus.GroupName(shardId))).PendingMessageCount);
            }
            finally
            {
                await db.KeyDeleteAsync([NotificationBus.StreamKey, dedupKey]);
            }
        }

        [RedisComponentFact]
        public async Task ConsumeLoopAutoClaimsFailedEntryWithoutWaitingForEmptyPolls()
        {
            var db = _fixture.Database;
            const int shardId = 9205;
            var dto = CreateNotification();
            string payload = JsonConvert.SerializeObject(dto);
            string dedupKey = NotificationDedupPolicy.TryGetKey(shardId, NotifyType.Youtube, payload);
            await RedisComponentFixture.AssertKeysAbsentAsync(db, NotificationBus.StreamKey, dedupKey);
            int dispatchCount = 0;
            using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            try
            {
                await NotificationBus.EnsureConsumerGroupAsync(db, shardId);
                await NotificationBus.PublishAsync(db, NotifyType.Youtube, dto);
                var consumer = new NotificationBusConsumer(
                    (_, _) =>
                    {
                        if (Interlocked.Increment(ref dispatchCount) == 1)
                            throw new InvalidOperationException("first dispatch fails");

                        cancellation.Cancel();
                        return Task.CompletedTask;
                    },
                    new NotificationBusConsumerOptions(
                        TimeSpan.FromMilliseconds(5),
                        TimeSpan.Zero,
                        TimeSpan.FromMinutes(5),
                        AutoClaimEveryPolls: 1,
                        BatchSize: 1));

                await consumer.ConsumeLoopAsync(db, shardId, cancellation.Token);

                Assert.Equal(2, dispatchCount);
                Assert.True(await db.KeyExistsAsync(dedupKey));
                Assert.Equal(0, (await db.StreamPendingAsync(
                    NotificationBus.StreamKey, NotificationBus.GroupName(shardId))).PendingMessageCount);
            }
            finally
            {
                await db.KeyDeleteAsync([NotificationBus.StreamKey, dedupKey]);
            }
        }

        [RedisComponentFact]
        public async Task AutoClaimCursorAdvancesPastPoisonHead()
        {
            var db = _fixture.Database;
            const int shardId = 9206;
            var poison = CreateNotification($"component-poison-{Guid.NewGuid():N}");
            var recoverable = CreateNotification($"component-recoverable-{Guid.NewGuid():N}");
            var poisonPayload = JsonConvert.SerializeObject(poison);
            var recoverablePayload = JsonConvert.SerializeObject(recoverable);
            string poisonDedupKey = NotificationDedupPolicy.TryGetKey(
                shardId, NotifyType.Youtube, poisonPayload);
            string recoverableDedupKey = NotificationDedupPolicy.TryGetKey(
                shardId, NotifyType.Youtube, recoverablePayload);
            await RedisComponentFixture.AssertKeysAbsentAsync(
                db, NotificationBus.StreamKey, poisonDedupKey, recoverableDedupKey);
            int poisonDispatchCount = 0;
            int recoverableDispatchCount = 0;
            using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            try
            {
                await NotificationBus.EnsureConsumerGroupAsync(db, shardId);
                await NotificationBus.PublishAsync(db, NotifyType.Youtube, poison);
                await NotificationBus.PublishAsync(db, NotifyType.Youtube, recoverable);
                Assert.Equal(2, (await NotificationBus.ReadNewAsync(db, shardId, 2)).Length);

                var consumer = new NotificationBusConsumer(
                    (_, json) =>
                    {
                        var dto = JsonConvert.DeserializeObject<YoutubeNotification>(json);
                        if (dto.VideoId == poison.VideoId)
                        {
                            Interlocked.Increment(ref poisonDispatchCount);
                            throw new InvalidOperationException("poison entry");
                        }

                        if (dto.VideoId == recoverable.VideoId)
                        {
                            Interlocked.Increment(ref recoverableDispatchCount);
                            cancellation.Cancel();
                        }

                        return Task.CompletedTask;
                    },
                    new NotificationBusConsumerOptions(
                        TimeSpan.FromMilliseconds(5),
                        TimeSpan.Zero,
                        TimeSpan.FromMinutes(5),
                        AutoClaimEveryPolls: 1,
                        BatchSize: 1));

                await consumer.ConsumeLoopAsync(db, shardId, cancellation.Token);

                Assert.True(poisonDispatchCount >= 1);
                Assert.Equal(1, recoverableDispatchCount);
                Assert.True(await db.KeyExistsAsync(recoverableDedupKey));
                Assert.Equal(1, (await db.StreamPendingAsync(
                    NotificationBus.StreamKey, NotificationBus.GroupName(shardId))).PendingMessageCount);
            }
            finally
            {
                await db.KeyDeleteAsync(
                    [NotificationBus.StreamKey, poisonDedupKey, recoverableDedupKey]);
            }
        }

        [RedisComponentFact]
        public async Task MissingPayloadIsAcknowledgedWithoutDispatch()
        {
            var db = _fixture.Database;
            const int shardId = 9207;
            await RedisComponentFixture.AssertKeysAbsentAsync(db, NotificationBus.StreamKey);
            int dispatchCount = 0;

            try
            {
                await NotificationBus.EnsureConsumerGroupAsync(db, shardId);
                await db.StreamAddAsync(
                    NotificationBus.StreamKey,
                    [new NameValueEntry(NotificationBus.FieldType, NotifyType.Youtube)]);
                var entry = Assert.Single(await NotificationBus.ReadNewAsync(db, shardId, 1));
                var consumer = new NotificationBusConsumer((_, _) =>
                {
                    Interlocked.Increment(ref dispatchCount);
                    return Task.CompletedTask;
                });

                await consumer.ProcessEntryAsync(db, shardId, entry);

                Assert.Equal(0, dispatchCount);
                Assert.Equal(0, (await db.StreamPendingAsync(
                    NotificationBus.StreamKey, NotificationBus.GroupName(shardId))).PendingMessageCount);
            }
            finally
            {
                await db.KeyDeleteAsync(NotificationBus.StreamKey);
            }
        }

        private static async Task<StreamEntry> CreatePendingEntryAsync(
            IDatabase db,
            int shardId,
            YoutubeNotification dto)
        {
            await NotificationBus.EnsureConsumerGroupAsync(db, shardId);
            var messageId = await NotificationBus.PublishAsync(db, NotifyType.Youtube, dto);
            var entry = Assert.Single(await NotificationBus.ReadNewAsync(db, shardId, 1));
            Assert.Equal(messageId, entry.Id);
            return entry;
        }

        private static YoutubeNotification CreateNotification(string videoId = null)
        {
            return new YoutubeNotification
            {
                NoticeType = YoutubeNoticeType.Start,
                VideoId = videoId ?? $"component-video-{Guid.NewGuid():N}",
                ChannelId = "component-channel",
                ChannelTitle = "Component Channel",
                VideoTitle = "Component Video",
                ScheduledStartTime = DateTime.UtcNow,
            };
        }
    }
}
