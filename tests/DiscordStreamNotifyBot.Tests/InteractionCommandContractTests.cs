using Discord;
using Discord.Interactions;
using DiscordStreamNotifyBot.Localization;
using DiscordStreamNotifyBot.SharedService.Youtube;
using System.Text.RegularExpressions;

namespace DiscordStreamNotifyBot.Tests
{
    public sealed class InteractionCommandContractTests
    {
        [Fact]
        public async Task CommandSignaturesAreStableAcrossInstances()
        {
            using InteractionMetadataFixture first = await InteractionMetadataFixture.CreateAsync();
            using InteractionMetadataFixture second = await InteractionMetadataFixture.CreateAsync();

            string globalSignature = first.Handler.CommandSignature;

            Assert.Matches(new Regex("^[0-9A-F]{64}$", RegexOptions.CultureInvariant), globalSignature);
            Assert.Equal(globalSignature, first.Handler.CommandSignature);
            Assert.Equal(globalSignature, second.Handler.CommandSignature);
        }

        [Fact]
        public async Task ReadableCommandContractMatchesSnapshot()
        {
            using InteractionMetadataFixture fixture = await InteractionMetadataFixture.CreateAsync();
            string snapshotPath = Path.Combine(
                AppContext.BaseDirectory, "Snapshots", "InteractionCommands.contract.snap");
            string expected = File.ReadAllText(snapshotPath).ReplaceLineEndings("\n").TrimEnd();

            Assert.Equal(expected, fixture.Handler.ReadableCommandContract);
        }

        [Fact]
        public async Task SetLanguageCommandKeepsItsParameterAndChoiceContract()
        {
            using InteractionMetadataFixture fixture = await InteractionMetadataFixture.CreateAsync();
            SlashCommandInfo command = fixture.Interactions.SlashCommands.Single(command =>
                command.Module.SlashGroupName == "server-admin" && command.Name == "set-language");
            SlashCommandParameterInfo parameter = Assert.Single(command.Parameters);

            Assert.Equal("language", parameter.Name);
            Assert.Equal(typeof(string), parameter.ParameterType);
            Assert.True(parameter.IsRequired);
            Assert.Collection(parameter.Choices.OrderBy(choice => choice.Name, StringComparer.Ordinal),
                choice =>
                {
                    Assert.Equal("English", choice.Name);
                    Assert.Equal(SupportedLocale.English, choice.Value);
                },
                choice =>
                {
                    Assert.Equal("Japanese", choice.Name);
                    Assert.Equal(SupportedLocale.Japanese, choice.Value);
                },
                choice =>
                {
                    Assert.Equal("Traditional Chinese", choice.Name);
                    Assert.Equal(SupportedLocale.TraditionalChinese, choice.Value);
                });
        }

        [Fact]
        public async Task SharedAdministratorCommandsUseDedicatedServerAdminGroup()
        {
            using InteractionMetadataFixture fixture = await InteractionMetadataFixture.CreateAsync();
            SlashCommandInfo[] commands = fixture.Interactions.SlashCommands
                .Where(command => command.Module.SlashGroupName == "server-admin")
                .ToArray();

            Assert.Equal([
                "send-message-to-bot-owner",
                "set-global-notice-channel",
                "set-language"
            ], commands.Select(command => command.Name).OrderBy(name => name, StringComparer.Ordinal));
            Assert.All(commands, command =>
            {
                Assert.Equal(GuildPermission.Administrator, command.DefaultMemberPermissions);
                Assert.Equal(GuildPermission.Administrator, command.Module.DefaultMemberPermissions);
            });
            Assert.DoesNotContain(fixture.Interactions.SlashCommands, command =>
                command.Module.SlashGroupName == "utility" &&
                command.DefaultMemberPermissions == GuildPermission.Administrator);
        }

        [Fact]
        public async Task YoutubeSetMessageKeepsParameterOrderTypesAndOptionality()
        {
            using InteractionMetadataFixture fixture = await InteractionMetadataFixture.CreateAsync();
            SlashCommandInfo command = fixture.Interactions.SlashCommands.Single(command =>
                command.Module.SlashGroupName == "youtube" && command.Name == "set-message");

            Assert.Collection(command.Parameters,
                parameter => AssertParameter(parameter, "channel", typeof(string), true),
                parameter => AssertParameter(parameter, "notification-type", typeof(YoutubeStreamService.NoticeType), true),
                parameter => AssertParameter(parameter, "message", typeof(string), false));
        }

        [Theory]
        [InlineData("twitch", GuildPermission.ManageMessages)]
        [InlineData("twitch-spider", GuildPermission.Administrator)]
        [InlineData("youtube-spider", GuildPermission.Administrator)]
        public async Task RestrictedGroupsKeepPermissionMetadataOnEveryLeaf(
            string groupName,
            GuildPermission expectedPermission)
        {
            using InteractionMetadataFixture fixture = await InteractionMetadataFixture.CreateAsync();
            SlashCommandInfo[] commands = fixture.Interactions.SlashCommands
                .Where(command => command.Module.SlashGroupName == groupName)
                .ToArray();

            Assert.NotEmpty(commands);
            Assert.All(commands, command => Assert.Equal(expectedPermission, command.DefaultMemberPermissions));
        }

        private static void AssertParameter(
            SlashCommandParameterInfo parameter,
            string expectedName,
            Type expectedType,
            bool expectedRequired)
        {
            Assert.Equal(expectedName, parameter.Name);
            Assert.Equal(expectedType, parameter.ParameterType);
            Assert.Equal(expectedRequired, parameter.IsRequired);
        }
    }
}
