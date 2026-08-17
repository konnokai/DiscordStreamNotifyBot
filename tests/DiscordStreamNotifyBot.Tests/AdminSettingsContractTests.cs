using Discord;
using DiscordStreamNotifyBot.Shared;
using DiscordStreamNotifyBot.Shared.Messages;
using DiscordStreamNotifyBot.SharedService.AdminSettings;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DiscordStreamNotifyBot.Tests
{
    public sealed class AdminSettingsContractTests
    {
        private const string Snowflake = "18446744073709551615";

        [Fact]
        public void EnvelopeUsesCamelCaseAndStringSnowflakes()
        {
            var json = JObject.Parse(JsonConvert.SerializeObject(new AdminSettingsRequestEnvelope
            {
                ContractVersion = 1,
                CorrelationId = Guid.NewGuid().ToString("N"),
                GuildId = Snowflake,
                ActorUserId = Snowflake,
                Action = AdminSettingsContract.YoutubeRemoveAction,
                Payload = JObject.FromObject(new { source = "UC123" })
            }));

            Assert.Equal(JTokenType.String, json["guildId"]!.Type);
            Assert.Equal(Snowflake, json.Value<string>("guildId"));
            Assert.Equal(JTokenType.String, json["actorUserId"]!.Type);
            Assert.Equal(Snowflake, json.Value<string>("actorUserId"));
            Assert.Equal(
                ["action", "actorUserId", "contractVersion", "correlationId", "guildId", "payload"],
                json.Properties().Select(x => x.Name).OrderBy(x => x, StringComparer.Ordinal));
        }

        [Fact]
        public void SnapshotUsesAllowlistedCamelCaseShapeAndStringChannelIds()
        {
            var snapshot = new AdminSettingsSnapshot
            {
                Capabilities = ["guild.common"],
                Guild = new AdminSettingsGuild { Id = Snowflake, Name = "guild", MemberCount = 1 },
                Resources = new AdminSettingsResources
                {
                    Channels =
                    [
                        new AdminSettingsChannel
                        {
                            Id = Snowflake,
                            Name = "news",
                            Type = "news",
                            CanView = true,
                            CanSendMessages = true,
                            CanEmbedLinks = true,
                            CanManageEvents = false
                        }
                    ]
                }
            };

            var json = JObject.Parse(JsonConvert.SerializeObject(snapshot));
            Assert.Equal(
                ["capabilities", "common", "contractVersion", "crawlers", "guild", "health", "notifications", "resources", "verification"],
                json.Properties().Select(x => x.Name).OrderBy(x => x, StringComparer.Ordinal));
            Assert.Equal(JTokenType.String, json["guild"]!["id"]!.Type);
            Assert.Equal(JTokenType.String, json["resources"]!["channels"]![0]!["id"]!.Type);
            Assert.Equal("news", json["resources"]!["channels"]![0]!.Value<string>("type"));
        }

        [Fact]
        public void PendingReplySerializesWithoutLosingState()
        {
            var result = AdminSettingsMutationResult.Pending("settings.cleanup-pending");
            var json = JObject.Parse(JsonConvert.SerializeObject(new AdminSettingsCommandReply
            {
                CorrelationId = Guid.NewGuid().ToString("N"),
                ShardId = 2,
                State = result.State,
                Code = result.Code,
                Arguments = result.Arguments
            }));

            Assert.Equal(1, json.Value<int>("contractVersion"));
            Assert.Equal("pending", json.Value<string>("state"));
            Assert.Equal("settings.cleanup-pending", json.Value<string>("code"));
            Assert.Equal(JTokenType.Object, json["arguments"]!.Type);
        }

        [Fact]
        public void RedisChannelsMatchBackendContract()
        {
            Assert.Equal("cluster:admin-settings:snapshot:request", RedisChannels.AdminSettings.SnapshotRequest);
            Assert.Equal("cluster:admin-settings:command:request", RedisChannels.AdminSettings.CommandRequest);
            Assert.Equal("cluster:admin-settings:reply:abc123", RedisChannels.AdminSettings.Reply("abc123"));
        }

        [Theory]
        [InlineData("settings.snapshot", "Snapshot")]
        [InlineData("guild.set-locale", "Command")]
        [InlineData("guild.set-global-notice-channel", "Command")]
        [InlineData("guild.set-verification-log-channel", "Command")]
        [InlineData("youtube-notification.upsert", "Command")]
        [InlineData("youtube-notification.remove", "Command")]
        [InlineData("twitch-notification.upsert", "Command")]
        [InlineData("twitch-notification.remove", "Command")]
        [InlineData("twitcasting-notification.upsert", "Command")]
        [InlineData("twitcasting-notification.remove", "Command")]
        [InlineData("youtube-crawler.add", "Command")]
        [InlineData("youtube-crawler.remove", "Command")]
        [InlineData("twitch-crawler.add", "Command")]
        [InlineData("twitch-crawler.remove", "Command")]
        [InlineData("twitcasting-crawler.add", "Command")]
        [InlineData("twitcasting-crawler.remove", "Command")]
        [InlineData("youtube-verification.upsert", "Command")]
        [InlineData("youtube-verification.remove", "Command")]
        [InlineData("youtube-verification.set-probe-video", "Command")]
        [InlineData("youtube-verification.use-automatic-probe", "Command")]
        [InlineData("twitch-verification.upsert", "Command")]
        [InlineData("twitch-verification.remove", "Command")]
        public void SupportedActionDispatchesToExpectedRoute(string action, string expected)
        {
            var request = Request(action);

            Assert.Equal(expected, AdminSettingsService.Classify(request, _ => true, _ => true, out _, out _).ToString());
        }

        [Fact]
        public void VersionUnsupportedAndNonOwningDecisionsAreSafe()
        {
            var unsupported = Request(AdminSettingsContract.SetLocaleAction);
            unsupported.ContractVersion = 2;
            var notOwned = Request(AdminSettingsContract.SetLocaleAction);
            var unknown = Request("unknown.action");

            Assert.Equal(AdminSettingsService.RequestRoute.UnsupportedVersion,
                AdminSettingsService.Classify(unsupported, _ => true, _ => true, out _, out _));
            Assert.Equal(AdminSettingsService.RequestRoute.Ignore,
                AdminSettingsService.Classify(notOwned, _ => true, _ => false, out _, out _));
            Assert.Equal(AdminSettingsService.RequestRoute.UnsupportedAction,
                AdminSettingsService.Classify(unknown, _ => true, _ => true, out _, out _));
        }

        [Fact]
        public void PayloadValidationRequiresSnowflakeAndAllMessageKeys()
        {
            Assert.True(AdminSettingsService.TryParseChannelId("", true, out ulong unset));
            Assert.Equal(0UL, unset);
            Assert.False(AdminSettingsService.TryParseChannelId("", false, out _));
            Assert.False(AdminSettingsService.TryParseChannelId("not-a-snowflake", false, out _));

            var valid = JsonConvert.DeserializeObject<AdminYoutubeMessagesPayload>(
                "{\"newStream\":\"\",\"newVideo\":\"\",\"start\":\"\",\"end\":\"\",\"changeTime\":\"\",\"delete\":\"\"}");
            var missing = JsonConvert.DeserializeObject<AdminYoutubeMessagesPayload>("{\"start\":\"x\"}");
            Assert.True(AdminSettingsService.Valid(valid));
            Assert.False(AdminSettingsService.Valid(missing));

            Assert.True(AdminSettingsService.ValidYoutubeUpsertPayload(JObject.Parse(
                "{\"source\":\"holo\",\"streamChannelId\":\"1\",\"videoChannelId\":\"2\",\"createEvent\":false," +
                "\"messages\":{\"newStream\":\"\",\"newVideo\":\"\",\"start\":\"\",\"end\":\"\",\"changeTime\":\"\",\"delete\":\"\"}}")));
            Assert.False(AdminSettingsService.ValidYoutubeUpsertPayload(JObject.Parse(
                "{\"source\":\"holo\",\"streamChannelId\":1,\"videoChannelId\":\"2\",\"createEvent\":false,\"messages\":{}}")));
        }

        [Fact]
        public void RequestParserRejectsNumericSnowflakes()
        {
            string correlationId = Guid.NewGuid().ToString("N");
            string valid = $"{{\"contractVersion\":1,\"correlationId\":\"{correlationId}\",\"guildId\":\"1\",\"actorUserId\":\"2\",\"action\":\"settings.snapshot\",\"payload\":{{}}}}";
            string numeric = $"{{\"contractVersion\":1,\"correlationId\":\"{correlationId}\",\"guildId\":1,\"actorUserId\":2,\"action\":\"settings.snapshot\",\"payload\":{{}}}}";

            Assert.True(AdminSettingsService.TryReadRequest(valid, out _));
            Assert.False(AdminSettingsService.TryReadRequest(numeric, out _));
        }

        [Theory]
        [InlineData(null, 2, 2)]
        [InlineData(0, 2, 2)]
        [InlineData(3, 2, 3)]
        public void CrawlerLimitPreservesPlatformFallback(int? configured, int fallback, int expected)
            => Assert.Equal(expected, CrawlerPolicy.ResolveLimit((uint?)configured, fallback));

        [Theory]
        [InlineData(1, 1, false, 0, 500, true)]
        [InlineData(2, 1, true, 0, 500, true)]
        [InlineData(2, 1, false, 500, 500, true)]
        [InlineData(2, 1, false, 499, 500, false)]
        public void CrawlerMemberRequirementPreservesOwnerAndOfficialExceptions(
            ulong actor, ulong owner, bool official, int members, int required, bool expected)
            => Assert.Equal(expected, CrawlerPolicy.HasGeneralEligibility(actor, owner, official, members, required));

        [Theory]
        [InlineData(10, 10, false, true)]
        [InlineData(11, 10, false, false)]
        [InlineData(11, 10, true, true)]
        public void CrawlerRemovalRequiresOwningGuildUnlessBotOwner(
            ulong ownerGuild, ulong guild, bool botOwner, bool expected)
            => Assert.Equal(expected, CrawlerPolicy.CanRemove(ownerGuild, guild, botOwner));

        [Fact]
        public void CrawlerOwnerNotificationsKeepPlatformManagementRoutes()
        {
            var youtube = CrawlerOwnerNotifier.BuildAddedMessage(
                CrawlerPlatform.Youtube, "source", "Channel", "source", "Guild", "Actor");
            var twitch = CrawlerOwnerNotifier.BuildAddedMessage(
                CrawlerPlatform.Twitch, "source", "Channel", "source", "Guild", "Actor");
            var twitcasting = CrawlerOwnerNotifier.BuildAddedMessage(
                CrawlerPlatform.Twitcasting, "source", "Channel", "source", "Guild", "Actor");
            Assert.Equal("已新增 YouTube 頻道爬蟲", youtube.Embed.Title);
            Assert.Contains(youtube.Embed.Fields, field => field.Name == "認可頻道");
            Assert.Equal(
                ["spider_youtube:trusted:source", "spider_youtube:untrusted:source",
                    "spider_youtube:record:source", "spider_youtube:unrecord:source"],
                ButtonIds(youtube.Components));
            Assert.Equal(
                ["spider_twitch:warning:source", "spider_twitch:record:source"],
                ButtonIds(twitch.Components));
            Assert.Equal(
                ["spider_tc:warning:source", "spider_tc:record:source"],
                ButtonIds(twitcasting.Components));
        }

        private static string[] ButtonIds(MessageComponent components)
            => components.Components.OfType<ActionRowComponent>()
                .SelectMany(row => row.Components)
                .OfType<ButtonComponent>()
                .Select(button => button.CustomId)
                .ToArray();

        [Theory]
        [InlineData("youtube-crawler.add", "{\"source\":\"UC1\"}", true)]
        [InlineData("youtube-crawler.remove", "{\"sourceId\":\"UC1\"}", true)]
        [InlineData("youtube-crawler.remove", "{\"sourceId\":1}", false)]
        [InlineData("youtube-verification.upsert", "{\"source\":\"UC1\",\"roleId\":\"123\"}", true)]
        [InlineData("youtube-verification.upsert", "{\"source\":\"UC1\",\"roleId\":123}", false)]
        [InlineData("youtube-verification.set-probe-video", "{\"sourceId\":\"UC1\",\"video\":\"abc\"}", true)]
        public void NewPayloadShapesRequireStringIdentifiers(string action, string json, bool expected)
            => Assert.Equal(expected, AdminSettingsService.ValidCrawlerOrVerificationPayload(action, JObject.Parse(json)));

        [Theory]
        [InlineData(true, false, false, 9, 10, true)]
        [InlineData(false, false, false, 9, 10, false)]
        [InlineData(true, true, false, 0, 10, false)]
        [InlineData(true, false, true, 9, 10, false)]
        [InlineData(true, false, false, 10, 10, false)]
        public void RoleResourceExplainsWhetherBotCanManage(
            bool permission, bool everyone, bool managed, int position, int botPosition, bool expected)
            => Assert.Equal(expected,
                AdminSettingsService.CanManageRole(permission, everyone, managed, position, botPosition));

        [Fact]
        public void ExpandedSnapshotKeepsRoleAndNullableIdsAsStrings()
        {
            var snapshot = new AdminSettingsSnapshot
            {
                Resources = new AdminSettingsResources
                {
                    Roles = [new AdminSettingsRole { Id = Snowflake, Name = "role", BotCanManage = true }]
                },
                Crawlers = new AdminSettingsCrawlers
                {
                    Youtube = new AdminSettingsCrawlerPlatform
                    {
                        Enabled = true,
                        Count = 1,
                        Limit = 3,
                        Items = [new AdminSettingsCrawlerItem { SourceId = "UC1", SourceName = "channel" }]
                    }
                },
                Verification = new AdminSettingsVerification
                {
                    Youtube =
                    [
                        new AdminSettingsYoutubeVerification
                        {
                            SourceId = "UC1",
                            RoleId = Snowflake,
                            PreviousRoleId = null
                        }
                    ]
                }
            };

            JObject json = JObject.Parse(JsonConvert.SerializeObject(snapshot));
            Assert.Equal(JTokenType.String, json["resources"]!["roles"]![0]!["id"]!.Type);
            Assert.Equal(JTokenType.Null, json["verification"]!["youtube"]![0]!["previousRoleId"]!.Type);
            Assert.Equal(3, json["crawlers"]!["youtube"]!["limit"]!.Value<int>());
        }

        private static AdminSettingsRequestEnvelope Request(string action)
            => new()
            {
                ContractVersion = 1,
                CorrelationId = Guid.NewGuid().ToString("N"),
                GuildId = "1",
                ActorUserId = "2",
                Action = action,
                Payload = new JObject()
            };
    }
}
