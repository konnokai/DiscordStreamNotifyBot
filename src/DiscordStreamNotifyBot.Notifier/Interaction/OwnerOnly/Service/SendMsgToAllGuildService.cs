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

                await modal.FollowupAsync(text: $"本次發送的類型為: {GetNoticeTypeDisplayName(noticeType)}", embed: BuildEmbed(payload), components: component.Build(), ephemeral: true);
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
                    await button.SendErrorAsync("你無法使用本功能", true);
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

                    await button.FollowupAsync("已廣播至所有 shard，各 shard 將對自己持有的伺服器發送（進度請見各 shard log）", ephemeral: true);
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
                .WithTitle("來自開發者消息")
                .WithDescription(payload.Message)
                .WithImageUrl(payload.ImageUrl)
                .WithFooter("管理員可以透過 `/utility set-global-notice-channel` 來設定由哪個頻道來接收小幫手相關的通知");

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
                    try
                    {
                        List<KeyValuePair<ulong, ulong>> list = db.GuildConfig
                            .Distinct((x) => x.GuildId)
                            .Where((x) => x.NoticeChannelId != 0)
                            .Select((x) => new KeyValuePair<ulong, ulong>(x.GuildId, x.NoticeChannelId))
                            .ToList();

                        int i = 0, num = list.Count;
                        foreach (var item in list)
                        {
                            i++;

                            var guild = _client.GetGuild(item.Key);
                            if (guild == null)
                            {
                                Log.Warn($"伺服器不存在: {item.Key}");
                                try
                                {
                                    db.GuildConfig.RemoveRange(db.GuildConfig.Where((x) => x.GuildId == item.Key));
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
                                Log.Warn($"頻道不存在: {guild.Name} / {item.Value}");
                                try
                                {
                                    db.GuildConfig.RemoveRange(db.GuildConfig.Where((x) => x.NoticeChannelId == item.Value));
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
                                        Log.Warn($"全球訊息通知 | {guild.Name} / {textChannel.Name} 發送失敗，將於 {timeSpan.TotalSeconds} 秒後重試 (第 {retryAttempt} 次重試)");
                                        return timeSpan;
                                    })
                                    .ExecuteAsync(async () =>
                                    {
                                        await textChannel.SendMessageAsync(embed: embed);
                                        isSendMessageGuildId.Add(item.Key);
                                    });
                            }
                            catch (Discord.Net.HttpException ex) when (ex.DiscordCode.HasValue && ex.DiscordCode == DiscordErrorCode.MissingPermissions ||
                                ex.DiscordCode == DiscordErrorCode.InsufficientPermissions)
                            {
                                Log.Warn($"缺少權限導致無法傳送訊息至: {guild.Name} / {textChannel.Name}");
                                db.GuildConfig.Single((x) => x.GuildId == guild.Id).NoticeChannelId = 0;
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
                        Log.Error(ex.Demystify(), "Send Message To Global Notice Channel Error");
                    }

                    db.SaveChanges();
                    Log.Info("已於全球訊息專用通知頻道發送完成");
                }
                else if (noticeType == NoticeType.Sponsor)
                {
                    foreach (var item in DiscordStreamNotifyBot.Utility.OfficialGuildList)
                    {
                        isSendMessageGuildId.Add(item);
                    }

                    Log.Info($"工商訊息已忽略的官方伺服器數: {isSendMessageGuildId.Count}");
                }

                try
                {
                    List<KeyValuePair<ulong, ulong>> list = db.NoticeYoutubeStreamChannel
                        .Distinct((x) => x.GuildId)
                        .Where((x) => !isSendMessageGuildId.Contains(x.GuildId) && _client.Guilds.Any((x2) => x2.Id == x.GuildId))
                        .Select((x) => new KeyValuePair<ulong, ulong>(x.GuildId, x.DiscordNoticeVideoChannelId))
                        .ToList();

                    int i = 0, num = list.Count;
                    foreach (var item in list)
                    {
                        i++;

                        var guild = _client.GetGuild(item.Key);
                        if (guild == null)
                        {
                            Log.Warn($"伺服器不存在: {item.Key}");
                            try
                            {
                                db.NoticeYoutubeStreamChannel.RemoveRange(db.NoticeYoutubeStreamChannel.Where((x) => x.GuildId == item.Key));
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
                            Log.Warn($"頻道不存在: {guild.Name} / {item.Value}");
                            try
                            {
                                db.NoticeYoutubeStreamChannel.RemoveRange(db.NoticeYoutubeStreamChannel.Where((x) => x.DiscordNoticeVideoChannelId == item.Value));
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
                                    Log.Warn($"全球訊息通知 | {guild.Name} / {textChannel.Name} 發送失敗，將於 {timeSpan.TotalSeconds} 秒後重試 (第 {retryAttempt} 次重試)");
                                    return timeSpan;
                                })
                                .ExecuteAsync(async () =>
                                {
                                    await textChannel.SendMessageAsync(embed: embed);
                                    isSendMessageGuildId.Add(item.Key);
                                });
                        }
                        catch (Discord.Net.HttpException ex) when (ex.DiscordCode.HasValue && ex.DiscordCode == DiscordErrorCode.MissingPermissions ||
                            ex.DiscordCode == DiscordErrorCode.InsufficientPermissions)
                        {
                            Log.Warn($"缺少權限導致無法傳送訊息至: {guild.Name} / {textChannel.Name}");
                            db.NoticeYoutubeStreamChannel.RemoveRange(db.NoticeYoutubeStreamChannel.Where((x) => x.DiscordNoticeVideoChannelId == item.Value));
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
                    Log.Error(ex.Demystify(), "Send Message To YouTube Notice Channel Error");
                }

                db.SaveChanges();
                Log.Info("已於 YT 通知頻道發送完成");

                try
                {
                    List<KeyValuePair<ulong, ulong>> list = db.NoticeTwitchStreamChannels
                        .Distinct((x) => x.GuildId)
                        .Where((x) => !isSendMessageGuildId.Contains(x.GuildId) && _client.Guilds.Any((x2) => x2.Id == x.GuildId))
                        .Select((x) => new KeyValuePair<ulong, ulong>(x.GuildId, x.DiscordChannelId))
                        .ToList();

                    int i = 0, num = list.Count;
                    foreach (var item in list)
                    {
                        i++;

                        var guild = _client.GetGuild(item.Key);
                        if (guild == null)
                        {
                            Log.Warn($"伺服器不存在: {item.Key}");
                            try
                            {
                                db.NoticeTwitchStreamChannels.RemoveRange(db.NoticeTwitchStreamChannels.Where((x) => x.GuildId == item.Key));
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
                            Log.Warn($"頻道不存在: {guild.Name} / {item.Value}");
                            try
                            {
                                db.NoticeTwitchStreamChannels.RemoveRange(db.NoticeTwitchStreamChannels.Where((x) => x.DiscordChannelId == item.Value));
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
                                    Log.Warn($"全球訊息通知 | {guild.Name} / {textChannel.Name} 發送失敗，將於 {timeSpan.TotalSeconds} 秒後重試 (第 {retryAttempt} 次重試)");
                                    return timeSpan;
                                })
                                .ExecuteAsync(async () =>
                                {
                                    await textChannel.SendMessageAsync(embed: embed);
                                    isSendMessageGuildId.Add(item.Key);
                                });
                        }
                        catch (Discord.Net.HttpException ex) when (ex.DiscordCode.HasValue && ex.DiscordCode == DiscordErrorCode.MissingPermissions ||
                            ex.DiscordCode == DiscordErrorCode.InsufficientPermissions)
                        {
                            Log.Warn($"缺少權限導致無法傳送訊息至: {guild.Name} / {textChannel.Name}");
                            db.NoticeTwitchStreamChannels.RemoveRange(db.NoticeTwitchStreamChannels.Where((x) => x.DiscordChannelId == item.Value));
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
                    Log.Error(ex.Demystify(), "Send Message To Twitch Notice Channel Error");
                }

                db.SaveChanges();
                Log.Info("已於 Twitch 通知頻道發送完成");

                try
                {
                    var memberList = db.GuildConfig
                        .Distinct((x) => x.GuildId)
                        .Where((x) => x.LogMemberStatusChannelId != 0 && !isSendMessageGuildId.Contains(x.GuildId) && _client.Guilds.Any((x2) => x2.Id == x.GuildId))
                        .Select((x) => new KeyValuePair<ulong, ulong>(x.GuildId, x.LogMemberStatusChannelId))
                        .ToList();

                    int i = 0, num = memberList.Count;
                    foreach (var item in memberList)
                    {
                        i++;

                        var guild = _client.GetGuild(item.Key);
                        if (guild == null)
                        {
                            Log.Warn($"伺服器不存在: {item.Key}");
                            try
                            {
                                db.GuildConfig.RemoveRange(db.GuildConfig.Where((x) => x.GuildId == item.Key));
                                db.GuildYoutubeMemberConfig.RemoveRange(db.GuildYoutubeMemberConfig.Where((x) => x.GuildId == item.Key));
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
                            Log.Warn($"頻道不存在: {guild.Name} / {item.Value}");
                            try
                            {
                                db.GuildConfig.RemoveRange(db.GuildConfig.Where((x) => x.GuildId == item.Key));
                                db.GuildYoutubeMemberConfig.RemoveRange(db.GuildYoutubeMemberConfig.Where((x) => x.GuildId == item.Key));
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
                                    Log.Warn($"全球訊息通知 | {guild.Name} / {textChannel.Name} 發送失敗，將於 {timeSpan.TotalSeconds} 秒後重試 (第 {retryAttempt} 次重試)");
                                    return timeSpan;
                                })
                                .ExecuteAsync(async () =>
                                {
                                    await textChannel.SendMessageAsync(embed: embed);
                                    isSendMessageGuildId.Add(item.Key);
                                });
                        }
                        catch (Discord.Net.HttpException ex) when (ex.DiscordCode.HasValue && ex.DiscordCode == DiscordErrorCode.MissingPermissions ||
                            ex.DiscordCode == DiscordErrorCode.InsufficientPermissions)
                        {
                            Log.Warn($"缺少權限導致無法傳送訊息至: {guild.Name} / {textChannel.Name}");
                            db.GuildConfig.RemoveRange(db.GuildConfig.Where((x) => x.GuildId == item.Key));
                            db.GuildYoutubeMemberConfig.RemoveRange(db.GuildYoutubeMemberConfig.Where((x) => x.GuildId == item.Key));
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
                    Log.Error(ex.Demystify(), "Send Message To YouTube Memeber Notice Channel Error");
                }

                db.SaveChanges();
                Log.Info("已於會限驗證紀錄頻道發送完成");

                isSending = false;
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