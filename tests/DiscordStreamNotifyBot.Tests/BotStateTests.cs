using DiscordStreamNotifyBot.Shared;

namespace DiscordStreamNotifyBot.Tests
{
    [CollectionDefinition(Name, DisableParallelization = true)]
    public sealed class BotStateCollectionDefinition
    {
        public const string Name = "BotState static state";
    }

    [Collection(BotStateCollectionDefinition.Name)]
    public sealed class BotStateTests : IDisposable
    {
        private readonly bool _originalIsConnect = BotState.IsConnect;
        private readonly int _originalShardId = BotState.ShardId;
        private readonly int _originalTotalShardCount = BotState.TotalShardCount;

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        public void OneOrFewerShardsOwnsEveryGuild(int totalShardCount)
        {
            BotState.ShardId = 99;
            BotState.TotalShardCount = totalShardCount;

            Assert.True(BotState.IsServerOnThisShard(ulong.MaxValue));
        }

        [Theory]
        [InlineData(0, 4, 0, true)]
        [InlineData(0, 4, 1, false)]
        [InlineData(3, 4, 3, true)]
        [InlineData(3, 4, 2, false)]
        [InlineData(11, 16, 11, true)]
        [InlineData(11, 16, 12, false)]
        public void IsServerOnThisShardUsesDiscordSnowflakeFormula(
            int shardId,
            int totalShardCount,
            int guildShard,
            bool expected)
        {
            BotState.ShardId = shardId;
            BotState.TotalShardCount = totalShardCount;
            var guildId = CreateGuildIdForShard(guildShard, totalShardCount);

            Assert.Equal(expected, BotState.IsServerOnThisShard(guildId));
        }

        [Theory]
        [InlineData(false, 2, false)]
        [InlineData(true, 2, true)]
        [InlineData(true, 1, false)]
        public void ShouldDeleteMissingGuildRequiresReadyStateAndShardOwnership(
            bool isConnect,
            int guildShard,
            bool expected)
        {
            BotState.IsConnect = isConnect;
            BotState.ShardId = 2;
            BotState.TotalShardCount = 4;
            var guildId = CreateGuildIdForShard(guildShard, BotState.TotalShardCount);

            Assert.Equal(expected, BotState.ShouldDeleteMissingGuild(guildId));
        }

        public void Dispose()
        {
            BotState.IsConnect = _originalIsConnect;
            BotState.ShardId = _originalShardId;
            BotState.TotalShardCount = _originalTotalShardCount;
        }

        private static ulong CreateGuildIdForShard(int shardId, int totalShardCount)
        {
            const ulong sequence = 123456;
            const ulong lowerSnowflakeBits = 0x2AAAAA;
            return ((sequence * (ulong)totalShardCount + (ulong)shardId) << 22) | lowerSnowflakeBits;
        }
    }
}
