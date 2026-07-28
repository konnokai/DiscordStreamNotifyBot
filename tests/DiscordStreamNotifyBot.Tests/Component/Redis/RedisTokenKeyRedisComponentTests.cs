using System.Collections.Concurrent;
using DiscordStreamNotifyBot.Shared;
using StackExchange.Redis;

namespace DiscordStreamNotifyBot.Tests.Component.Redis
{
    [Collection(RedisComponentCollection.Name)]
    [Trait("Category", "RedisComponent")]
    public sealed class RedisTokenKeyRedisComponentTests
    {
        private readonly RedisComponentFixture _fixture;

        public RedisTokenKeyRedisComponentTests(RedisComponentFixture fixture)
        {
            _fixture = fixture;
        }

        [RedisComponentFact]
        public async Task ConcurrentAuthoritiesAdoptSingleSetNxWinnerAndPublishIt()
        {
            var db = _fixture.Database;
            var subscriber = _fixture.Connection.GetSubscriber();
            RedisKey redisKey = RedisChannels.Cluster.RedisTokenKey;
            var channel = new RedisChannel(
                RedisChannels.Member.SyncRedisToken,
                RedisChannel.PatternMode.Literal);
            var publishedValues = new ConcurrentBag<string>();
            var firstPublication = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            var originalUtilityKey = Utility.RedisKey;
            await RedisComponentFixture.AssertKeysAbsentAsync(db, redisKey);

            await subscriber.SubscribeAsync(channel, (_, value) =>
            {
                var published = value.ToString();
                publishedValues.Add(published);
                firstPublication.TrySetResult(published);
            });

            try
            {
                RedisConnection.ResetForRetry(_fixture.Option);
                var configs = Enumerable.Range(0, 32).Select(_ => new BotConfig()).ToArray();

                await Task.WhenAll(configs.Select(config => RedisTokenKeyProvisioner.EnsureAsync(
                    BotRole.Notifier,
                    shardId: 0,
                    config,
                    TimeSpan.FromSeconds(2))));

                var winner = (await db.StringGetAsync(redisKey)).ToString();
                var publishedWinner = await firstPublication.Task.WaitAsync(TimeSpan.FromSeconds(5));

                Assert.NotEmpty(winner);
                Assert.Equal(128, winner.Length);
                Assert.All(configs, config => Assert.Equal(winner, config.RedisTokenKey));
                Assert.Equal(winner, Utility.RedisKey);
                Assert.Equal(winner, publishedWinner);
                Assert.Contains(winner, publishedValues);
            }
            finally
            {
                await subscriber.UnsubscribeAsync(channel);
                RedisConnection.ResetForRetry(_fixture.Option);
                Utility.RedisKey = originalUtilityKey;
                await db.KeyDeleteAsync(redisKey);
            }
        }

        [RedisComponentFact]
        public async Task NonAuthorityAdoptsCanonicalKeyInsteadOfOverwritingIt()
        {
            var db = _fixture.Database;
            RedisKey redisKey = RedisChannels.Cluster.RedisTokenKey;
            var canonicalKey = new string('a', 128);
            var conflictingKey = new string('b', 128);
            var originalUtilityKey = Utility.RedisKey;
            await RedisComponentFixture.AssertKeysAbsentAsync(db, redisKey);

            try
            {
                await db.StringSetAsync(redisKey, canonicalKey);
                RedisConnection.ResetForRetry(_fixture.Option);
                var config = new BotConfig { RedisTokenKey = conflictingKey };

                await RedisTokenKeyProvisioner.EnsureAsync(
                    BotRole.Notifier,
                    shardId: 1,
                    config,
                    TimeSpan.FromSeconds(2));

                Assert.Equal(canonicalKey, (await db.StringGetAsync(redisKey)).ToString());
                Assert.Equal(canonicalKey, config.RedisTokenKey);
                Assert.Equal(canonicalKey, Utility.RedisKey);
            }
            finally
            {
                RedisConnection.ResetForRetry(_fixture.Option);
                Utility.RedisKey = originalUtilityKey;
                await RedisComponentFixture.DeleteStringIfOwnedAsync(
                    db, redisKey, canonicalKey, conflictingKey);
            }
        }

        [RedisComponentFact]
        public async Task AuthorityMirrorsConfiguredKeyAndPublishesIt()
        {
            var db = _fixture.Database;
            var subscriber = _fixture.Connection.GetSubscriber();
            RedisKey redisKey = RedisChannels.Cluster.RedisTokenKey;
            var channel = new RedisChannel(
                RedisChannels.Member.SyncRedisToken,
                RedisChannel.PatternMode.Literal);
            var configuredKey = new string('c', 128);
            var publication = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            var originalUtilityKey = Utility.RedisKey;
            await RedisComponentFixture.AssertKeysAbsentAsync(db, redisKey);
            await subscriber.SubscribeAsync(channel, (_, value) =>
                publication.TrySetResult(value.ToString()));

            try
            {
                RedisConnection.ResetForRetry(_fixture.Option);
                var config = new BotConfig { RedisTokenKey = configuredKey };

                await RedisTokenKeyProvisioner.EnsureAsync(
                    BotRole.Notifier,
                    shardId: 0,
                    config,
                    TimeSpan.FromSeconds(2));

                Assert.Equal(configuredKey, (await db.StringGetAsync(redisKey)).ToString());
                Assert.Equal(configuredKey, config.RedisTokenKey);
                Assert.Equal(configuredKey, Utility.RedisKey);
                Assert.Equal(configuredKey, await publication.Task.WaitAsync(TimeSpan.FromSeconds(5)));
            }
            finally
            {
                await subscriber.UnsubscribeAsync(channel);
                RedisConnection.ResetForRetry(_fixture.Option);
                Utility.RedisKey = originalUtilityKey;
                await RedisComponentFixture.DeleteStringIfOwnedAsync(db, redisKey, configuredKey);
            }
        }
    }
}
