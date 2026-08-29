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
        public async Task CommandSignaturesAreStableAndIncludeDebugOnlyCommandsSeparately()
        {
            using InteractionMetadataFixture first = await InteractionMetadataFixture.CreateAsync();
            using InteractionMetadataFixture second = await InteractionMetadataFixture.CreateAsync();

            string globalSignature = first.Handler.CommandSignature;
            string debugSignature = first.Handler.DebugCommandSignature;

            Assert.Matches(new Regex("^[0-9A-F]{64}$", RegexOptions.CultureInvariant), globalSignature);
            Assert.Matches(new Regex("^[0-9A-F]{64}$", RegexOptions.CultureInvariant), debugSignature);
            Assert.Equal(globalSignature, first.Handler.CommandSignature);
            Assert.Equal(globalSignature, second.Handler.CommandSignature);
            Assert.NotEqual(globalSignature, debugSignature);
            Assert.Contains(first.Interactions.SlashCommands, command => command.Module.DontAutoRegister);
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
                "set-language",
                "set-verification-log-channel"
            ], commands.Select(command => command.Name).OrderBy(name => name, StringComparer.Ordinal));
            Assert.All(commands, command =>
            {
                Assert.Equal(GuildPermission.Administrator, command.DefaultMemberPermissions);
                Assert.Equal(GuildPermission.Administrator, command.Module.DefaultMemberPermissions);
            });
            Assert.DoesNotContain(fixture.Interactions.SlashCommands, command =>
                command.Module.SlashGroupName == "utility" &&
                command.DefaultMemberPermissions == GuildPermission.Administrator);

            SlashCommandInfo command = commands.Single(command => command.Name == "set-verification-log-channel");
            SlashCommandParameterInfo channel = Assert.Single(command.Parameters);
            AssertParameter(channel, "log-channel", typeof(ITextChannel), true);
            Assert.DoesNotContain(fixture.Interactions.SlashCommands, command =>
                command.Module.SlashGroupName == "youtube-member-set" &&
                command.Name == "set-notice-member-status-channel");
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

        [Fact]
        public async Task TwitchSubscriptionCommandsKeepCanonicalNamesPermissionsAndParameters()
        {
            using InteractionMetadataFixture fixture = await InteractionMetadataFixture.CreateAsync();
            string[] userCommands = fixture.Interactions.SlashCommands
                .Where(command => command.Module.SlashGroupName == "twitch-subscription")
                .Select(command => command.Name)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();
            Assert.Equal([
                "cancel-subscription-check",
                "check",
                "list-can-check-channel",
                "show-my-twitch-account"
            ], userCommands);

            SlashCommandInfo add = fixture.Interactions.SlashCommands.Single(command =>
                command.Module.SlashGroupName == "twitch-subscription-set" && command.Name == "add-subscription-check");
            Assert.Equal(GuildPermission.Administrator, add.DefaultMemberPermissions);
            Assert.Collection(add.Parameters,
                parameter => AssertParameter(parameter, "channel-url", typeof(string), true),
                parameter => AssertParameter(parameter, "role", typeof(IRole), true));

            SlashCommandInfo remove = fixture.Interactions.SlashCommands.Single(command =>
                command.Module.SlashGroupName == "twitch-subscription-set" && command.Name == "remove-subscription-check");
            Assert.Equal(GuildPermission.Administrator, remove.DefaultMemberPermissions);
            SlashCommandParameterInfo channel = Assert.Single(remove.Parameters);
            AssertParameter(channel, "channel", typeof(string), true);
            Assert.True(channel.IsAutocomplete);
        }

        [Fact]
        public async Task YoutubeMemberCommandsUseDedicatedGroupsAndPreserveLeafNames()
        {
            using InteractionMetadataFixture fixture = await InteractionMetadataFixture.CreateAsync();
            string[] userCommands = fixture.Interactions.SlashCommands
                .Where(command => command.Module.SlashGroupName == "youtube-member")
                .Select(command => command.Name)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();
            Assert.Equal([
                "cancel-member-check",
                "check",
                "list-can-check-channel",
                "show-my-youtube-account",
                "unlink"
            ], userCommands);

            string[] settingCommands = fixture.Interactions.SlashCommands
                .Where(command => command.Module.SlashGroupName == "youtube-member-set")
                .Select(command => command.Name)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();
            Assert.Equal([
                "add-member-check",
                "clear-check-video",
                "list-checked-member",
                "remove-member-check",
                "set-check-video"
            ], settingCommands);

            Assert.DoesNotContain(fixture.Interactions.SlashCommands,
                command => command.Module.SlashGroupName is "member" or "member-set");
        }

        [Theory]
        [InlineData("twitcasting", GuildPermission.ManageMessages)]
        [InlineData("twitch", GuildPermission.ManageMessages)]
        [InlineData("twitcasting-spider", GuildPermission.Administrator)]
        [InlineData("twitch-spider", GuildPermission.Administrator)]
        [InlineData("youtube-spider", GuildPermission.Administrator)]
        [InlineData("youtube-member-set", GuildPermission.Administrator)]
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
