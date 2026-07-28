using DiscordStreamNotifyBot.Shared.Messages;
using DiscordStreamNotifyBot.SharedService.Cluster;

namespace DiscordStreamNotifyBot.Tests
{
    public sealed class NoNotifyGuildFilterTests
    {
        [Fact]
        public void EmptyGuildCollectionReturnsEmptyResult()
        {
            var result = ClusterQueryService.FilterNoNotifyGuilds(
                Array.Empty<GuildSnapshot>(), Array.Empty<ulong>(), Array.Empty<ulong>());

            Assert.Empty(result);
        }

        [Fact]
        public void GuildsWithoutExclusionsAreSortedByMemberCountDescending()
        {
            var guilds = new[]
            {
                Guild(1, 10),
                Guild(2, 100),
                Guild(3, 0),
            };

            var result = ClusterQueryService.FilterNoNotifyGuilds(
                guilds, Array.Empty<ulong>(), Array.Empty<ulong>());

            Assert.Equal(new ulong[] { 2, 1, 3 }, result.Select(guild => guild.Id));
        }

        [Fact]
        public void ConfiguredAndOfficialGuildsAreExcluded()
        {
            var guilds = new[] { Guild(1, 10), Guild(2, 20), Guild(3, 30), Guild(4, 40) };

            var result = ClusterQueryService.FilterNoNotifyGuilds(
                guilds,
                new ulong[] { 1, 2, 2, 99 },
                new ulong[] { 2, 3, 100 });

            Assert.Equal(new ulong[] { 4 }, result.Select(guild => guild.Id));
        }

        [Fact]
        public void DuplicateCandidateGuildsRemainInResult()
        {
            var guilds = new[] { Guild(1, 10), Guild(1, 20) };

            var result = ClusterQueryService.FilterNoNotifyGuilds(
                guilds, Array.Empty<ulong>(), Array.Empty<ulong>());

            Assert.Equal(new[] { 20, 10 }, result.Select(guild => guild.MemberCount));
        }

        [Fact]
        public void EqualMemberCountsPreserveSourceOrder()
        {
            var guilds = new[] { Guild(3, 10), Guild(1, 10), Guild(2, 10) };

            var result = ClusterQueryService.FilterNoNotifyGuilds(
                guilds, Array.Empty<ulong>(), Array.Empty<ulong>());

            Assert.Equal(new ulong[] { 3, 1, 2 }, result.Select(guild => guild.Id));
        }

        [Fact]
        public void InputCollectionsAreNotModified()
        {
            var guilds = new List<GuildSnapshot> { Guild(1, 10), Guild(2, 20) };
            var configured = new List<ulong> { 1 };
            var official = new List<ulong> { 3 };

            _ = ClusterQueryService.FilterNoNotifyGuilds(guilds, configured, official);

            Assert.Equal(new ulong[] { 1, 2 }, guilds.Select(guild => guild.Id));
            Assert.Equal(new ulong[] { 1 }, configured);
            Assert.Equal(new ulong[] { 3 }, official);
        }

        [Fact]
        public void NullInputsAreRejected()
        {
            Assert.Throws<ArgumentNullException>(() => ClusterQueryService.FilterNoNotifyGuilds(
                null, Array.Empty<ulong>(), Array.Empty<ulong>()));
            Assert.Throws<ArgumentNullException>(() => ClusterQueryService.FilterNoNotifyGuilds(
                Array.Empty<GuildSnapshot>(), null, Array.Empty<ulong>()));
            Assert.Throws<ArgumentNullException>(() => ClusterQueryService.FilterNoNotifyGuilds(
                Array.Empty<GuildSnapshot>(), Array.Empty<ulong>(), null));
        }

        private static GuildSnapshot Guild(ulong id, int memberCount)
            => new() { Id = id, Name = $"Guild {id}", MemberCount = memberCount };
    }
}
