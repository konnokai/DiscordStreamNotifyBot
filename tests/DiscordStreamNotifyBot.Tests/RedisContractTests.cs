using DiscordStreamNotifyBot.Shared;
using StackExchange.Redis;

namespace DiscordStreamNotifyBot.Tests
{
    public sealed class RedisContractTests
    {
        [Fact]
        public void RedisChannelConstantsMatchContracts()
        {
            Assert.Equal("youtube.startstream", RedisChannels.Youtube.StartStream);
            Assert.Equal("youtube.endstream", RedisChannels.Youtube.EndStream);
            Assert.Equal("youtube.addstream", RedisChannels.Youtube.AddStream);
            Assert.Equal("youtube.deletestream", RedisChannels.Youtube.DeleteStream);
            Assert.Equal("youtube.unarchived", RedisChannels.Youtube.Unarchived);
            Assert.Equal("youtube.memberonly", RedisChannels.Youtube.MemberOnly);
            Assert.Equal("youtube.record", RedisChannels.Youtube.Record);
            Assert.Equal("youtube.429error", RedisChannels.Youtube.Error429);
            Assert.Equal("youtube.test", RedisChannels.Youtube.Test);
            Assert.Equal("youtube.newstream", RedisChannels.Youtube.NewStream);
            Assert.Equal("youtube.changestreamtime", RedisChannels.Youtube.ChangeStreamTime);
            Assert.Equal("youtube.otherStart", RedisChannels.Youtube.OtherStart);
            Assert.Equal("youtube.pubsub.CreateOrUpdate", RedisChannels.Youtube.PubSubCreateOrUpdate);
            Assert.Equal("youtube.pubsub.Deleted", RedisChannels.Youtube.PubSubDeleted);
            Assert.Equal("youtube.pubsub.NeedRegister", RedisChannels.Youtube.PubSubNeedRegister);
            Assert.Equal("youtube.nijisanji.liver.{affiliation}", RedisChannels.Youtube.NijisanjiLiverTemplate);

            Assert.Equal("twitch.record", RedisChannels.Twitch.Record);
            Assert.Equal("twitch:stream_online", RedisChannels.Twitch.StreamOnline);
            Assert.Equal("twitch:channel_update", RedisChannels.Twitch.ChannelUpdate);
            Assert.Equal("twitch:stream_offline", RedisChannels.Twitch.StreamOffline);
            Assert.Equal("twitch:authorization_changed", RedisChannels.Twitch.AuthorizationChanged);
            Assert.Equal("twitch:reconcile_requested", RedisChannels.Twitch.ReconcileRequested);
            Assert.Equal("twitch:webhook_secret", RedisChannels.Twitch.WebhookSecret);

            Assert.Equal("twitcasting.pubsub.startlive", RedisChannels.Twitcasting.PubSubStartLive);
            Assert.Equal("twitcasting.record", RedisChannels.Twitcasting.Record);
            Assert.Equal("member.revokeToken", RedisChannels.Member.RevokeToken);

            Assert.Equal("DiscordStreamBot:OfficialGuildList", RedisChannels.SharedState.OfficialGuildList);
            Assert.Equal("cluster:stats:guild_count", RedisChannels.SharedState.GuildCountHash);
            Assert.Equal("cluster:stats:member_count", RedisChannels.SharedState.MemberCountHash);
            Assert.Equal("cluster:stats:guild_snapshot", RedisChannels.SharedState.GuildSnapshotHash);

            Assert.Equal("notifier.control.shutdown", RedisChannels.Notifier.Shutdown);
            Assert.Equal("notifier.control.leaveGuild", RedisChannels.Notifier.LeaveGuild);
            Assert.Equal("notifier.control.leaveNoNotify", RedisChannels.Notifier.LeaveNoNotifyGuild);
            Assert.Equal("notifier.control.sendMessageToAll", RedisChannels.Notifier.SendMessageToAll);

            Assert.Equal("cluster:scraper:leader", RedisChannels.Cluster.ScraperLeader);
            Assert.Equal("cluster:total_shards", RedisChannels.Cluster.TotalShards);
            Assert.Equal("cluster:query:request", RedisChannels.Cluster.QueryRequest);
        }

        [Fact]
        public void RedisDynamicKeysMatchContracts()
        {
            Assert.Equal("twitch:stream_data:user-42", RedisChannels.Twitch.StreamData("user-42"));
            Assert.Equal("twitch:stream_notified:stream-99", RedisChannels.Twitch.StreamNotification("stream-99"));
            Assert.Equal("cluster:heartbeat:notifier:2", RedisChannels.Cluster.Heartbeat("notifier", "2"));
            Assert.Equal("cluster:shard:lease:7", RedisChannels.Cluster.ShardLease(7));
            Assert.Equal("cluster:query:reply:correlation-id", RedisChannels.Cluster.QueryReply("correlation-id"));
            Assert.Equal("twitch:oauth:refresh-lock:user-42", RedisChannels.OAuth.TwitchRefreshLock("user-42"));
            Assert.Equal(1, RedisChannels.OAuth.DatabaseNumber);
        }

        [Fact]
        public void NotificationBusConstantsAndNamesMatchContracts()
        {
            Assert.Equal("bot:notify", NotificationBus.StreamKey);
            Assert.Equal("type", NotificationBus.FieldType);
            Assert.Equal("payload", NotificationBus.FieldPayload);
            Assert.Equal(10000, NotificationBus.MaxApproxLength);
            Assert.Equal("shard-3", NotificationBus.GroupName(3));
            Assert.Equal("notifier-3", NotificationBus.ConsumerName(3));
        }

        [Fact]
        public void TryGetPayloadReturnsTypeAndPayload()
        {
            var entry = new StreamEntry("1-0", new NameValueEntry[]
            {
                new(NotificationBus.FieldType, "youtube"),
                new(NotificationBus.FieldPayload, "{\"VideoId\":\"abc\"}")
            });

            var result = NotificationBus.TryGetPayload(entry, out var type, out var payload);

            Assert.True(result);
            Assert.Equal("youtube", type);
            Assert.Equal("{\"VideoId\":\"abc\"}", payload);
        }

        [Theory]
        [InlineData(null, "{}")]
        [InlineData("youtube", null)]
        [InlineData("", "{}")]
        [InlineData("youtube", "")]
        public void TryGetPayloadRejectsMissingOrEmptyFields(string type, string payload)
        {
            var fields = new List<NameValueEntry>();
            if (type != null)
                fields.Add(new NameValueEntry(NotificationBus.FieldType, type));
            if (payload != null)
                fields.Add(new NameValueEntry(NotificationBus.FieldPayload, payload));
            var entry = new StreamEntry("1-0", fields.ToArray());

            Assert.False(NotificationBus.TryGetPayload(entry, out _, out _));
        }
    }
}
