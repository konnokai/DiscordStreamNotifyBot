using Discord.Interactions;
using DiscordStreamNotifyBot.DataBase;
using DiscordStreamNotifyBot.Shared;
using Polly;
using System.Net;
using System.Reflection;

namespace DiscordStreamNotifyBot.Interaction.OwnerOnly.Service
{
    public class SendMsgToAllGuildService : IInteractionService
    {
        public enum NoticeType
        {
            [ChoiceDisplay("一般")]
            Normal,
            [ChoiceDisplay("工商")]
            Sponsor
        }

        private class ButtonCheckData
        {
            public ulong UserId { get; set; }
            public ulong ChannelId { get; set; }
            public string Guid { get; set; }
            public SendAllPayload Payload { get; set; }

            public ButtonCheckData(ulong userId, ulong channelId, string guid, SendAllPayload payload)
            {
                UserId = userId;
                ChannelId = channelId;
                Guid = guid;
                Payload = payload;
            }
        }

        /// <summary>跨 shard 全球訊息發送的結構化參數（傳結構化資料，各 shard 端重建 embed，符合計畫 §8）。</summary>
        private class SendAllPayload
        {
            public NoticeType NoticeType { get; set; }
            public string Message { get; set; }
            public string ImageUrl { get; set; }
            public string AuthorName { get; set; }
            public string AuthorIconUrl { get; set; }
        }

        private readonly DiscordSocketClient _client;
        private readonly MainDbService _dbService;
        private ButtonCheckData checkData;
        private bool isSending = false;

        public SendMsgToAllGuildService(DiscordSocketClient discordSocketClient, MainDbService dbService)
        {
            _client = discordSocketClient;
            _dbService = dbService;

            // 各 shard 訂閱發送廣播：對自己持有的伺服器發送（_client.Guilds 過濾天然不重複）
            Bot.RedisSub.Subscribe(new RedisChannel(RedisChannels.Notifier.SendMessageToAll, RedisChannel.PatternMode.Literal), (_, value) =>
            {
                try
                {
                    var payload = JsonConvert.DeserializeObject<SendAllPayload>(value);
                    if (payload == null)
                        return;

                    ThreadPool.QueueUserWorkItem(async (state) => await StartSendMessage(payload));
                }
                catch (Exception ex)
                {
                    Log.Error(ex.Demystify(), "SendMessageToAll 廣播處理失敗");
                }
            });

            _client.ModalSubmitted += async modal =>
            {
                if (modal.Data.CustomId != "send_message")
                    return;

                await modal.DeferAsync(true);

                List<SocketMessageComponentData> components = modal.Data.Components.ToList();

                var noticeType = Enum.Parse<NoticeType>(modal.Data.Components
                    .First(x => x.CustomId == "notice_type").Value);
                string imageUrl = (modal.Data.Attachments?.Count ?? 0) == 1 ? modal.Data.Attachments.First().Url : "";
                string message = components.First(x => x.CustomId == "message").Value;

                var payload = new SendAllPayload
                {
                    NoticeType = noticeType,
                    Message = message,
                    ImageUrl = imageUrl,
                    AuthorName = modal.User.ToString(),
                    AuthorIconUrl = modal.User.GetDisplayAvatarUrl() ?? modal.User.GetDefaultAvatarUrl()
                };

                var guid = Guid.NewGuid().ToString().Replace("-", "");
                ComponentBuilder component = new ComponentBuilder()
                    .WithButton("是", $"{guid}-yes", ButtonStyle.Success)
                    .WithButton("否", $"{guid}-no", ButtonStyle.Danger);

                await modal.FollowupAsync(text: $"本次傳送類型：{GetNoticeTypeDisplayName(noticeType)}", embed: BuildEmbed(payload), components: component.Build(), ephemeral: true);
                checkData = new ButtonCheckData(modal.User.Id, modal.ChannelId.Value, guid, payload);
            };

            _client.ButtonExecuted += async button =>
            {
                if (checkData == null || isSending)
                    return;

                if (!button.Data.CustomId.StartsWith(checkData.Guid))
                    return;

                if (!(button is SocketMessageComponent userMsg) ||
                    !(userMsg.Channel is ITextChannel chan) ||
                    userMsg.User.Id != checkData.UserId ||
                    userMsg.Channel.Id != checkData.ChannelId)
                {
                    await button.SendErrorAsync("你沒有權限使用本功能", true);
                    return;
                }

                try
                {
                    await button.UpdateAsync((x) => x.Components = new ComponentBuilder()
                        .WithButton("是", $"{checkData.Guid}-yes", ButtonStyle.Success, disabled: true)
                        .WithButton("否", $"{checkData.Guid}-no", ButtonStyle.Danger, disabled: true).Build());
                }
                catch { }

                if (button.Data.CustomId.EndsWith("yes"))
                {
                    // 廣播給所有 shard（含本 shard），各自對自己持有的伺服器發送，避免只發單一 shard
                    await Bot.RedisSub.PublishAsync(
                        new RedisChannel(RedisChannels.Notifier.SendMessageToAll, RedisChannel.PatternMode.Literal),
                        JsonConvert.SerializeObject(checkData.Payload));

                    await button.FollowupAsync("已廣播至所有分片。每個分片會向其持有的伺服器傳送訊息；請查看各分片日誌。", ephemeral: true);
                    checkData = null;
                }
                else
                {
                    await button.SendErrorAsync("已取消發送", true);
                }
            };
        }

        /// <summary>由結構化參數重建發送用的 embed（各 shard 端執行）。</summary>
        private static Embed BuildEmbed(SendAllPayload payload)
        {
            var eb = new EmbedBuilder().WithOkColor()
                .WithUrl("https://konnokai.me/")
                .WithTitle("來自開發者的訊息")
                .WithDescription(payload.Message)
                .WithImageUrl(payload.ImageUrl)
                .WithFooter("管理員可透過 `/server-admin set-global-notice-channel` 設定接收小幫手通知的頻道");

            if (!string.IsNullOrEmpty(payload.AuthorName))
                eb.WithAuthor(payload.AuthorName, payload.AuthorIconUrl);

            return eb.Build();
        }

        private async Task StartSendMessage(SendAllPayload payload)
        {
            if (isSending)
                return;

            isSending = true;
            Embed embed = BuildEmbed(payload);
            var noticeType = payload.NoticeType;
            var isSendMessageGuildId = new HashSet<ulong>();
            using (var db = _dbService.GetDbContext())
            {
                if (noticeType == NoticeType.Normal)
                {
                    // 跨 shard：只處理本 shard 持有的伺服器，否則對別 shard 的伺服器 GetGuild 會是 null 而誤刪其 GuildConfig（與 YT/Twitch/會限段一致）
                    await SendToTargetsAsync(db.GuildConfig
                            .AsEnumerable()
                            .DistinctBy((x) => x.GuildId)
                            .Where((x) => x.NoticeChannelId != 0 && _client.Guilds.Any((x2) => x2.Id == x.GuildId))
                            .Select((x) => new KeyValuePair<ulong, ulong>(x.GuildId, x.NoticeChannelId)),
                        embed, isSendMessageGuildId,
                        onGuildMissing: (guildId, channelId) => db.GuildConfig.RemoveRange(db.GuildConfig.Where((x) => x.GuildId == guildId)),
                        onChannelMissing: (guildId, channelId) => db.GuildConfig.RemoveRange(db.GuildConfig.Where((x) => x.NoticeChannelId == channelId)),
                        onForbidden: (guildId, channelId) => db.GuildConfig.Single((x) => x.GuildId == guildId).NoticeChannelId = 0,
                        errorLogMessage: "Send Message To Global Notice Channel Error");

                    db.SaveChanges();
                    Log.Info("已於全球訊息專用通知頻道發送完成");
                }
                else if (noticeType == NoticeType.Sponsor)
                {
                    foreach (var item in DiscordStreamNotifyBot.Utility.OfficialGuildList)
                    {
                        isSendMessageGuildId.Add(item);
                    }

                    Log.Info($"工商訊息已忽略的官方伺服器數：{isSendMessageGuildId.Count}");
                }

                await SendToTargetsAsync(db.NoticeYoutubeStreamChannel
                        .AsEnumerable()
                        .DistinctBy((x) => x.GuildId)
                        .Where((x) => !isSendMessageGuildId.Contains(x.GuildId) && _client.Guilds.Any((x2) => x2.Id == x.GuildId))
                        .Select((x) => new KeyValuePair<ulong, ulong>(x.GuildId, x.DiscordNoticeVideoChannelId)),
                    embed, isSendMessageGuildId,
                    onGuildMissing: (guildId, channelId) => db.NoticeYoutubeStreamChannel.RemoveRange(db.NoticeYoutubeStreamChannel.Where((x) => x.GuildId == guildId)),
                    onChannelMissing: (guildId, channelId) => db.NoticeYoutubeStreamChannel.RemoveRange(db.NoticeYoutubeStreamChannel.Where((x) => x.DiscordNoticeVideoChannelId == channelId)),
                    onForbidden: (guildId, channelId) => db.NoticeYoutubeStreamChannel.RemoveRange(db.NoticeYoutubeStreamChannel.Where((x) => x.DiscordNoticeVideoChannelId == channelId)),
                    errorLogMessage: "Send Message To YouTube Notice Channel Error");

                db.SaveChanges();
                Log.Info("已於 YouTube 通知頻道傳送完成");

                await SendToTargetsAsync(db.NoticeTwitchStreamChannels
                        .AsEnumerable()
                        .DistinctBy((x) => x.GuildId)
                        .Where((x) => !isSendMessageGuildId.Contains(x.GuildId) && _client.Guilds.Any((x2) => x2.Id == x.GuildId))
                        .Select((x) => new KeyValuePair<ulong, ulong>(x.GuildId, x.DiscordChannelId)),
                    embed, isSendMessageGuildId,
                    onGuildMissing: (guildId, channelId) => db.NoticeTwitchStreamChannels.RemoveRange(db.NoticeTwitchStreamChannels.Where((x) => x.GuildId == guildId)),
                    onChannelMissing: (guildId, channelId) => db.NoticeTwitchStreamChannels.RemoveRange(db.NoticeTwitchStreamChannels.Where((x) => x.DiscordChannelId == channelId)),
                    onForbidden: (guildId, channelId) => db.NoticeTwitchStreamChannels.RemoveRange(db.NoticeTwitchStreamChannels.Where((x) => x.DiscordChannelId == channelId)),
                    errorLogMessage: "Send Message To Twitch Notice Channel Error");

                db.SaveChanges();
                Log.Info("已於 Twitch 通知頻道發送完成");

                // 會限紀錄頻道：伺服器、頻道不存在或缺少權限時，皆移除該伺服器的 GuildConfig 與 GuildYoutubeMemberConfig
                void RemoveGuildMemberConfig(ulong guildId, ulong channelId)
                {
                    db.GuildConfig.RemoveRange(db.GuildConfig.Where((x) => x.GuildId == guildId));
                    db.GuildYoutubeMemberConfig.RemoveRange(db.GuildYoutubeMemberConfig.Where((x) => x.GuildId == guildId));
                }

                await SendToTargetsAsync(db.GuildConfig
                        .AsEnumerable()
                        .DistinctBy((x) => x.GuildId)
                        .Where((x) => x.VerificationLogChannelId != 0 && !isSendMessageGuildId.Contains(x.GuildId) && _client.Guilds.Any((x2) => x2.Id == x.GuildId))
                        .Select((x) => new KeyValuePair<ulong, ulong>(x.GuildId, x.VerificationLogChannelId)),
                    embed, isSendMessageGuildId,
                    onGuildMissing: RemoveGuildMemberConfig,
                    onChannelMissing: RemoveGuildMemberConfig,
                    onForbidden: RemoveGuildMemberConfig,
                    errorLogMessage: "YouTube 會員驗證通知頻道傳送失敗");

                db.SaveChanges();
                Log.Info("已於 YouTube 會員驗證紀錄頻道傳送完成");

                isSending = false;
            }
        }

        /// <summary>
        /// 依序對 <paramref name="targets"/>（伺服器 Id → 頻道 Id）發送全球訊息，成功後把伺服器 Id 加入 <paramref name="isSendMessageGuildId"/>。
        /// <paramref name="targets"/> 須為延遲查詢，於本方法的 try 內才實體化，讓查詢失敗也由同一個 catch 記錄。
        /// 伺服器或頻道不存在時的清理失敗只記錄錯誤並繼續；缺少權限時的清理不另外 catch，失敗會中止本階段剩餘目標。
        /// </summary>
        private async Task SendToTargetsAsync(IEnumerable<KeyValuePair<ulong, ulong>> targets, Embed embed, HashSet<ulong> isSendMessageGuildId,
            Action<ulong, ulong> onGuildMissing, Action<ulong, ulong> onChannelMissing, Action<ulong, ulong> onForbidden, string errorLogMessage)
        {
            try
            {
                List<KeyValuePair<ulong, ulong>> list = targets.ToList();

                int i = 0, num = list.Count;
                foreach (var item in list)
                {
                    i++;

                    var guild = _client.GetGuild(item.Key);
                    if (guild == null)
                    {
                        Log.Warn($"伺服器不存在：{item.Key}");
                        try
                        {
                            onGuildMissing(item.Key, item.Value);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex.ToString());
                        }
                        continue;
                    }

                    var textChannel = guild.GetTextChannel(item.Value);
                    if (textChannel == null)
                    {
                        Log.Warn($"頻道不存在：{guild.Name} / {item.Value}");
                        try
                        {
                            onChannelMissing(item.Key, item.Value);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex.ToString());
                        }
                        continue;
                    }

                    try
                    {
                        await Policy.Handle<TimeoutException>()
                            .Or<Discord.Net.HttpException>((httpEx) => httpEx.HttpCode == HttpStatusCode.GatewayTimeout)
                            .Or<WebException>((ex) => ex.Message.Contains("unavailable")) // Resource temporarily unavailable
                            .WaitAndRetryAsync(3, (retryAttempt) =>
                            {
                                var timeSpan = TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));
                                Log.Warn($"全球訊息通知 | {guild.Name} / {textChannel.Name} 發送失敗，將於 {timeSpan.TotalSeconds} 秒後重試（第 {retryAttempt} 次）");
                                return timeSpan;
                            })
                            .ExecuteAsync(async () =>
                            {
                                await textChannel.SendMessageAsync(embed: embed);
                                isSendMessageGuildId.Add(item.Key);
                            });
                    }
                    catch (Discord.Net.HttpException ex) when (ex.DiscordCode == DiscordErrorCode.MissingPermissions ||
                        ex.DiscordCode == DiscordErrorCode.InsufficientPermissions)
                    {
                        Log.Warn($"缺少權限導致無法傳送訊息至：{guild.Name} / {textChannel.Name}");
                        onForbidden(item.Key, item.Value);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.Demystify(), $"MSG: {guild.Name} / {textChannel.Name}");
                    }
                    finally
                    {
                        Log.Info($"({i}/{num}) {item.Key}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), errorLogMessage);
            }
        }

        internal string GetNoticeTypeDisplayName(NoticeType noticeType)
        {
            return noticeType.GetType()
                .GetField(noticeType.ToString())
                .GetCustomAttribute<ChoiceDisplayAttribute>()
                .Name;
        }
    }
}
