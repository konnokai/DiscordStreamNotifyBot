namespace DiscordStreamNotifyBot.SharedService.Youtube
{
    public partial class YoutubeStreamService
    {
        private async Task ChangeGuildBannerAsync(string channelId, string videoId, bool fromBus = false)
        {
#if DEBUG || DEBUG_DONTREGISTERCOMMAND
            return;
#endif
            // 通知一律經匯流排：偵測端（Scraper）publish banner 事件，由消費端（Notifier）執行（需 GetGuild）
            if (!fromBus)
            {
                try
                {
                    await Shared.NotificationBusPublisher.PublishJsonAsync(_botConfig.RabbitMQ,
                        Shared.Messages.NotifyRoutingKeys.Banner,
                        new Shared.Messages.BannerChangeNotification { ChannelId = channelId, VideoId = videoId }).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Log.Error(ex.Demystify(), $"PublishBannerChange: {channelId} / {videoId}");
                }
                return;
            }

            List<DataBase.Table.BannerChange> list;

            using (var db = _dbService.GetDbContext())
            {
                list = _dbService.GetDbContext().BannerChange.AsNoTracking()
                    .Where(x => x.ChannelId == channelId)
                    .ToList();
            }

            if (list.Count == 0) return;

            foreach (var item in list)
            {
                try
                {
                    var guild = _client.GetGuild(item.GuildId);
                    if (guild == null)
                    {
                        // 多 Shard 環境：非本 Shard 持有的伺服器，或尚未 Ready，皆靜默略過，避免互刪設定
                        if (!Bot.ShouldDeleteMissingGuild(item.GuildId))
                            continue;

                        Log.Warn($"Guild not found: {item.GuildId} / {channelId} / {videoId}");
                        using (var db = _dbService.GetDbContext())
                        {
                            db.BannerChange.Remove(item);
                            await db.SaveChangesAsync();
                        }
                        continue;
                    }

                    if (guild.PremiumTier < PremiumTier.Tier2) continue;

                    if (videoId != item.LastChangeStreamId)
                    {
                        MemoryStream memStream;
                        try
                        {
                            memStream = new MemoryStream(await _httpClientFactory.CreateClient("").GetByteArrayAsync($"https://i.ytimg.com/vi/{videoId}/maxresdefault.jpg"));
                            if (memStream.Length < 2048) memStream = null;
                        }
                        catch (Exception ex)
                        {
                            Log.Error($"DownloadGuildBanner - {item.GuildId}\n" +
                                $"{channelId} / {videoId}\n" +
                                $"{ex.Message}\n" +
                                $"{ex.StackTrace}");
                            continue;
                        }

                        try
                        {
                            if (memStream != null)
                            {
                                Image image = new Image(memStream);
                                await guild.ModifyAsync((func) => func.Banner = image);
                            }

                            item.LastChangeStreamId = videoId;

                            using (var db = _dbService.GetDbContext())
                            {
                                db.BannerChange.Update(item);
                                await db.SaveChangesAsync();
                            }

                            Log.Info("ChangeGuildBanner" + (memStream == null ? "(Without Change)" : "") + $": {item.GuildId} / {videoId}");
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex.Demystify(), $"ChangeGuildBanner - {item.GuildId}: {channelId} / {videoId}");
                            continue;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex.Demystify(), $"ChangeGuildBanner - {item.GuildId}");
                    continue;
                }
            }
        }
    }
}
