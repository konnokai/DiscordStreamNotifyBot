using DiscordStreamNotifyBot.DataBase;

namespace DiscordStreamNotifyBot.Command.Admin
{
    public class AdministrationService : ICommandService
    {
        private string _reloadOfficialGuildListKey = "DiscordStreamBot:Admin:ReloadOfficialGuildList";
        private readonly DiscordSocketClient _client;
        private readonly MainDbService _dbService;

        public AdministrationService(DiscordSocketClient client, MainDbService service)
        {
            _client = client;
            _dbService = service;

            Bot.RedisSub.Subscribe(new RedisChannel(_reloadOfficialGuildListKey, RedisChannel.PatternMode.Literal), async (_, _) =>
            {
                try
                {
                    // 階段 5：白名單存於 Redis（跨 shard 共享），收到變更通知後重新載入
                    await Utility.LoadOfficialGuildListFromRedisAsync();
                    Log.Info($"官方伺服器白名單已重新載入（{Utility.OfficialGuildList.Count} 筆）");
                }
                catch (Exception ex)
                {
                    Log.Error(ex.Demystify(), "ReloadOfficialGuildList Error");
                }
            });
        }

        public async Task ClearUser(ITextChannel textChannel)
        {
            IEnumerable<IMessage> msgs = (await textChannel.GetMessagesAsync(100).FlattenAsync().ConfigureAwait(false))
                  .Where((item) => item.Author.Id == _client.CurrentUser.Id);

            await Task.WhenAll(Task.Delay(1000), textChannel.DeleteMessagesAsync(msgs)).ConfigureAwait(false);
        }

        /// <summary>儲存白名單至 Redis 並廣播變更，讓所有 shard 重新載入（階段 5）。</summary>
        internal async Task<bool> SaveAndBroadcastOfficialGuildListAsync()
        {
            try
            {
                if (!await Utility.SaveOfficialGuildListToRedisAsync())
                    return false;

                await Bot.RedisSub.PublishAsync(new RedisChannel(_reloadOfficialGuildListKey, RedisChannel.PatternMode.Literal), "");
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), "SaveAndBroadcastOfficialGuildList Error");
                return false;
            }

            return true;
        }

        internal IReadOnlyCollection<SocketGuild> GetNoNotifyGuilds()
        {
            var guilds = new List<SocketGuild>(_client.Guilds);
            using var db = _dbService.GetDbContext();

            db.NoticeYoutubeStreamChannel
                .AsEnumerable()
                .DistinctBy((x) => x.GuildId)
                .Select((x) => x.GuildId)
                .ToList()
                .ForEach((x) =>
                {
                    var guild = guilds.SingleOrDefault((x2) => x2.Id == x);
                    if (guild != null)
                        guilds.Remove(guild);
                });

            db.NoticeTwitchStreamChannels
                .AsEnumerable()
                .DistinctBy((x) => x.GuildId)
                .Select((x) => x.GuildId)
                .ToList()
                .ForEach((x) =>
                {
                    var guild = guilds.SingleOrDefault((x2) => x2.Id == x);
                    if (guild != null)
                        guilds.Remove(guild);
                });

            db.NoticeTwitcastingStreamChannels
                .AsEnumerable()
                .DistinctBy((x) => x.GuildId)
                .Select((x) => x.GuildId)
                .ToList()
                .ForEach((x) =>
                {
                    var guild = guilds.SingleOrDefault((x2) => x2.Id == x);
                    if (guild != null)
                        guilds.Remove(guild);
                });

            db.GuildYoutubeMemberConfig
                .AsEnumerable()
                .DistinctBy((x) => x.GuildId)
                .Select((x) => x.GuildId)
                .ToList()
                .ForEach((x) =>
                {
                    var guild = guilds.SingleOrDefault((x2) => x2.Id == x);
                    if (guild != null)
                        guilds.Remove(guild);
                });

            Utility.OfficialGuildList
                .ToList()
                .ForEach((x) =>
                {
                    var guild = guilds.SingleOrDefault((x2) => x2.Id == x);
                    if (guild != null)
                        guilds.Remove(guild);
                });

            guilds = guilds
                .OrderByDescending((x) => x.MemberCount)
                .ToList();

            return guilds.AsReadOnly();
        }
    }
}
