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
                command.Module.SlashGroupName == "utility" && command.Name == "set-language");
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
