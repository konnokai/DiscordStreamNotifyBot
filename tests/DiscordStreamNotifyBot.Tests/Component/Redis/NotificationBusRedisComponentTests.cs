using DiscordStreamNotifyBot.Shared;
using StackExchange.Redis;

namespace DiscordStreamNotifyBot.Tests.Component.Redis
{
    [Collection(RedisComponentCollection.Name)]
    [Trait("Category", "RedisComponent")]
    public sealed class NotificationBusRedisComponentTests
    {
        private readonly RedisComponentFixture _fixture;

        public NotificationBusRedisComponentTests(RedisComponentFixture fixture)
        {
            _fixture = fixture;
        }

        [RedisComponentFact]
        public async Task EnsureConsumerGroupUsesMkStreamAndIsIdempotent()
        {
            var db = _fixture.Database;
            const int shardId = 9101;
            await RedisComponentFixture.AssertKeysAbsentAsync(db, NotificationBus.StreamKey);

            try
            {
                await NotificationBus.EnsureConsumerGroupAsync(db, shardId);

                Assert.True(await db.KeyExistsAsync(NotificationBus.StreamKey));
                var firstGroups = await db.StreamGroupInfoAsync(NotificationBus.StreamKey);
                var firstGroup = Assert.Single(firstGroups);
                Assert.Equal(NotificationBus.GroupName(shardId), firstGroup.Name.ToString());

                await NotificationBus.EnsureConsumerGroupAsync(db, shardId);

                var secondGroups = await db.StreamGroupInfoAsync(NotificationBus.StreamKey);
                Assert.Single(secondGroups);
            }
            finally
            {
                await db.KeyDeleteAsync(NotificationBus.StreamKey);
            }
        }

        [RedisComponentFact]
        public async Task GroupCreatedAfterMessageReadsFromBeginningTracksPelAndAcks()
        {
            var db = _fixture.Database;
            const int shardId = 9102;
            await RedisComponentFixture.AssertKeysAbsentAsync(db, NotificationBus.StreamKey);

            try
            {
                var messageId = await NotificationBus.PublishAsync(db, "component.before-group", new { Value = 42 });
                await NotificationBus.EnsureConsumerGroupAsync(db, shardId);

                var entries = await NotificationBus.ReadNewAsync(db, shardId, 10);

                var entry = Assert.Single(entries);
                Assert.Equal(messageId, entry.Id);
                Assert.True(NotificationBus.TryGetPayload(entry, out var type, out var payload));
                Assert.Equal("component.before-group", type);
                Assert.Contains("\"Value\":42", payload);

                var pending = await db.StreamPendingAsync(NotificationBus.StreamKey, NotificationBus.GroupName(shardId));
                Assert.Equal(1, pending.PendingMessageCount);

                Assert.Equal(1, await NotificationBus.AckAsync(db, shardId, messageId));
                pending = await db.StreamPendingAsync(NotificationBus.StreamKey, NotificationBus.GroupName(shardId));
                Assert.Equal(0, pending.PendingMessageCount);
            }
            finally
            {
                await db.KeyDeleteAsync(NotificationBus.StreamKey);
            }
        }

        [RedisComponentFact]
        public async Task PendingMessageSurvivesConnectionRestartAndCanBeAutoClaimed()
        {
            var db = _fixture.Database;
            const int shardId = 9103;
            await RedisComponentFixture.AssertKeysAbsentAsync(db, NotificationBus.StreamKey);

            try
            {
                await NotificationBus.EnsureConsumerGroupAsync(db, shardId);
                var messageId = await NotificationBus.PublishAsync(db, "component.restart", new { Value = "pending" });

                using (var crashedConnection = await _fixture.OpenConnectionAsync())
                {
                    var crashedDb = crashedConnection.GetDatabase();
                    var delivered = await NotificationBus.ReadNewAsync(crashedDb, shardId, 1);
                    Assert.Equal(messageId, Assert.Single(delivered).Id);
                }

                using var restartedConnection = await _fixture.OpenConnectionAsync();
                var restartedDb = restartedConnection.GetDatabase();
                var claimed = await NotificationBus.AutoClaimAsync(restartedDb, shardId, TimeSpan.Zero, 10);

                Assert.Equal(messageId, Assert.Single(claimed).Id);
                Assert.Equal(1, await NotificationBus.AckAsync(restartedDb, shardId, messageId));
                var pending = await restartedDb.StreamPendingAsync(
                    NotificationBus.StreamKey,
                    NotificationBus.GroupName(shardId));
                Assert.Equal(0, pending.PendingMessageCount);
            }
            finally
            {
                await db.KeyDeleteAsync(NotificationBus.StreamKey);
            }
        }

        [RedisComponentFact]
        public async Task TwoShardGroupsReceiveAndAcknowledgeTheSameMessageIndependently()
        {
            var db = _fixture.Database;
            const int firstShardId = 9104;
            const int secondShardId = 9105;
            await RedisComponentFixture.AssertKeysAbsentAsync(db, NotificationBus.StreamKey);

            try
            {
                await NotificationBus.EnsureConsumerGroupAsync(db, firstShardId);
                await NotificationBus.EnsureConsumerGroupAsync(db, secondShardId);
                var messageId = await NotificationBus.PublishAsync(db, "component.broadcast", new { Value = "all-shards" });

                var first = Assert.Single(await NotificationBus.ReadNewAsync(db, firstShardId, 1));
                var second = Assert.Single(await NotificationBus.ReadNewAsync(db, secondShardId, 1));
                Assert.Equal(messageId, first.Id);
                Assert.Equal(messageId, second.Id);

                Assert.Equal(1, await NotificationBus.AckAsync(db, firstShardId, messageId));
                Assert.Equal(0, (await db.StreamPendingAsync(
                    NotificationBus.StreamKey,
                    NotificationBus.GroupName(firstShardId))).PendingMessageCount);
                Assert.Equal(1, (await db.StreamPendingAsync(
                    NotificationBus.StreamKey,
                    NotificationBus.GroupName(secondShardId))).PendingMessageCount);

                Assert.Equal(1, await NotificationBus.AckAsync(db, secondShardId, messageId));
            }
            finally
            {
                await db.KeyDeleteAsync(NotificationBus.StreamKey);
            }
        }

        [RedisComponentFact]
        public async Task PublishOnceAtomicallyCreatesOneMessageAndOneDedupMarker()
        {
            var db = _fixture.Database;
            RedisKey dedupKey = $"component:redis:notification:dedup:{Guid.NewGuid():N}";
            await RedisComponentFixture.AssertKeysAbsentAsync(db, NotificationBus.StreamKey, dedupKey);

            try
            {
                var publishes = Enumerable.Range(0, 32)
                    .Select(_ => NotificationBus.PublishOnceAsync(
                        db,
                        dedupKey,
                        TimeSpan.FromMinutes(5),
                        "component.publish-once",
                        new { Value = "same-event" }))
                    .ToArray();

                var messageIds = await Task.WhenAll(publishes);

                Assert.All(messageIds, messageId => Assert.Equal(messageIds[0], messageId));
                Assert.Equal(1, await db.StreamLengthAsync(NotificationBus.StreamKey));
                Assert.Equal(messageIds[0], await db.StringGetAsync(dedupKey));
                Assert.True((await db.KeyTimeToLiveAsync(dedupKey)) > TimeSpan.Zero);
                Assert.Single(await db.StreamRangeAsync(NotificationBus.StreamKey));
            }
            finally
            {
                await db.KeyDeleteAsync(new RedisKey[] { NotificationBus.StreamKey, dedupKey });
            }
        }
    }
}
