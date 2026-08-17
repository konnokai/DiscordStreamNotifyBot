using DiscordStreamNotifyBot.DataBase;
using DiscordStreamNotifyBot.DataBase.Table;
using DiscordStreamNotifyBot.Interaction;
using DiscordStreamNotifyBot.Localization;
using Microsoft.Extensions.DependencyInjection;
using System.Data;
using System.Management;
using System.Reflection;

namespace DiscordStreamNotifyBot.Interaction
{
    static class Extensions
    {
        private static readonly IEmote arrow_left = new Emoji("\u2B05");
        private static readonly IEmote arrow_right = new Emoji("\u27A1");

        // WithOkColor/WithErrorColor/WithRecordColor/ConvertDateTimeToDiscordMarkdown/
        // GetProductionType/GetProductionName 已移至 Shared 的 SharedExtensions（同命名空間 Interaction，
        // 供 Scraper 偵測層共用，計畫 §3-3）；此處刪除以免與其重複定義（擴充方法模稜兩可）。

        public static string GetCommandLine(this Process process)
        {
            if (!OperatingSystem.IsWindows()) return "";

            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT CommandLine FROM Win32_Process WHERE ProcessId = " + process.Id))
                using (ManagementObjectCollection objects = searcher.Get())
                {
                    return objects.Cast<ManagementBaseObject>().SingleOrDefault()?["CommandLine"]?.ToString();
                }
            }
            catch
            {
                return "";
            }
        }

        public static IEnumerable<T> Distinct<T, V>(this IEnumerable<T> source, Func<T, V> keySelector)
        {
            return source.Distinct(new CommonEqualityComparer<T, V>(keySelector));
        }

        public static bool HasStreamVideoByVideoId(string videoId)
        {
            videoId = videoId.Trim();

            using var db = Bot.DbService.GetDbContext();
            if (db.HoloVideos.AsNoTracking().Any((x) => x.VideoId == videoId)) return true;
            if (db.NijisanjiVideos.AsNoTracking().Any((x) => x.VideoId == videoId)) return true;
            if (db.OtherVideos.AsNoTracking().Any((x) => x.VideoId == videoId)) return true;
            if (db.NonApprovedVideos.AsNoTracking().Any((x) => x.VideoId == videoId)) return true;

            return false;
        }

        public static DataBase.Table.Video GetStreamVideoByVideoId(string videoId)
        {
            videoId = videoId.Trim();

            using var db = Bot.DbService.GetDbContext();
            if (db.HoloVideos.AsNoTracking().Any((x) => x.VideoId == videoId))
                return db.HoloVideos.AsNoTracking().First((x) => x.VideoId == videoId);
            if (db.NijisanjiVideos.AsNoTracking().Any((x) => x.VideoId == videoId))
                return db.NijisanjiVideos.AsNoTracking().First((x) => x.VideoId == videoId);
            if (db.OtherVideos.AsNoTracking().Any((x) => x.VideoId == videoId))
                return db.OtherVideos.AsNoTracking().First((x) => x.VideoId == videoId);
            if (db.NonApprovedVideos.AsNoTracking().Any((x) => x.VideoId == videoId))
                return db.NonApprovedVideos.AsNoTracking().First((x) => x.VideoId == videoId);

            return null;
        }

        // 依直播開始時間排序可能無法正確處理聊天用待機室，暫時保留此函式供後續評估。
        public static DataBase.Table.Video GetLastStreamVideoByChannelId(string channelId)
        {
            channelId = channelId.Trim();

            using var db = Bot.DbService.GetDbContext();
            if (db.HoloVideos.AsNoTracking().Any((x) => x.ChannelId == channelId))
                return db.HoloVideos.AsNoTracking().OrderByDescending((x) => x.ScheduledStartTime).First((x) => x.ChannelId == channelId);
            if (db.NijisanjiVideos.AsNoTracking().Any((x) => x.ChannelId == channelId))
                return db.NijisanjiVideos.AsNoTracking().OrderByDescending((x) => x.ScheduledStartTime).First((x) => x.ChannelId == channelId);
            if (db.OtherVideos.AsNoTracking().Any((x) => x.ChannelId == channelId))
                return db.OtherVideos.AsNoTracking().OrderByDescending((x) => x.ScheduledStartTime).First((x) => x.ChannelId == channelId);
            if (db.NonApprovedVideos.AsNoTracking().Any((x) => x.ChannelId == channelId))
                return db.NonApprovedVideos.AsNoTracking().OrderByDescending((x) => x.ScheduledStartTime).First((x) => x.ChannelId == channelId);

            return null;
        }

        public static bool IsChannelInDb(string channelId)
        {
            channelId = channelId.Trim();

            using var db = Bot.DbService.GetDbContext();
            if (db.HoloVideos.AsNoTracking().Any((x) => x.ChannelId == channelId)) return true;
            if (db.NijisanjiVideos.AsNoTracking().Any((x) => x.ChannelId == channelId)) return true;
            if (db.OtherVideos.AsNoTracking().Any((x) => x.ChannelId == channelId)) return true;
            if (db.NonApprovedVideos.AsNoTracking().Any((x) => x.ChannelId == channelId)) return true;

            return false;
        }

        public static string GetYoutubeChannelTitleByChannelId(this MainDbContext _, string channelId)
        {
            channelId = channelId.Trim();

            using var db = Bot.DbService.GetDbContext();

            YoutubeChannelSpider youtubeChannelSpider;
            if ((youtubeChannelSpider = db.YoutubeChannelSpider.AsNoTracking().FirstOrDefault((x) => x.ChannelId == channelId)) != null)
                return youtubeChannelSpider.ChannelTitle;

            if (db.HoloVideos.AsNoTracking().Any((x) => x.ChannelId == channelId))
                return db.HoloVideos.AsNoTracking().OrderByDescending((x) => x.ScheduledStartTime).First((x) => x.ChannelId == channelId).ChannelTitle;
            if (db.NijisanjiVideos.AsNoTracking().Any((x) => x.ChannelId == channelId))
                return db.NijisanjiVideos.AsNoTracking().OrderByDescending((x) => x.ScheduledStartTime).First((x) => x.ChannelId == channelId).ChannelTitle;
            if (db.OtherVideos.AsNoTracking().Any((x) => x.ChannelId == channelId))
                return db.OtherVideos.AsNoTracking().OrderByDescending((x) => x.ScheduledStartTime).First((x) => x.ChannelId == channelId).ChannelTitle;

            return channelId;
        }

        // GetNonApprovedChannelTitleByChannelId 已移至 Shared 的 SharedExtensions（供偵測層共用）。

        public static string GetTwitCastingChannelTitleByScreenId(this MainDbContext _, string screenId)
        {
            screenId = screenId.Trim();

            using var db = Bot.DbService.GetDbContext();

            TwitcastingSpider twitcastingSpider;
            if ((twitcastingSpider = db.TwitcastingSpider.AsNoTracking().FirstOrDefault((x) => x.ScreenId == screenId)) != null)
                return twitcastingSpider.ChannelTitle;

            return screenId;
        }

        public static string GetTwitchUserNameByUserId(this MainDbContext _, string userId)
        {
            userId = userId.Trim();

            using var db = Bot.DbService.GetDbContext();

            TwitchSpider twitchSpider;
            if ((twitchSpider = db.TwitchSpider.AsNoTracking().FirstOrDefault((x) => x.UserId == userId)) != null)
                return twitchSpider.UserName;

            return userId;
        }

        public static Task SendConfirmAsync(this IDiscordInteraction di, string des, bool isFollowerup = false, bool ephemeral = false)
        {
            if (isFollowerup || di.HasResponded)
            {
                return di.FollowupAsync(embed: new EmbedBuilder().WithOkColor().WithDescription(des).Build(), ephemeral: ephemeral);
            }

            return di.RespondAsync(embed: new EmbedBuilder().WithOkColor().WithDescription(des).Build(), ephemeral: ephemeral);
        }

        public static async Task<string> ResolveLocaleAsync(this IDiscordInteraction interaction,
            IServiceProvider services, bool isPrivate)
        {
            var guildLocaleService = services.GetRequiredService<GuildLocaleService>();
            var localeResolver = services.GetRequiredService<LocaleResolver>();
            var client = services.GetRequiredService<DiscordSocketClient>();
            string guildLocale = null;
            if (interaction.GuildId is ulong guildId)
                guildLocale = await guildLocaleService.GetAsync(guildId, client.GetGuild(guildId));

            return isPrivate
                ? localeResolver.ResolvePrivate(interaction.UserLocale, guildLocale, interaction.GuildLocale)
                : localeResolver.ResolvePublic(guildLocale, interaction.GuildLocale);
        }

        public static Task SendConfirmAsync(this IDiscordInteraction di, string title, string des, bool isFollowerup = false, bool ephemeral = false)
        {
            if (isFollowerup || di.HasResponded)
                return di.FollowupAsync(embed: new EmbedBuilder().WithOkColor().WithTitle(title).WithDescription(des).Build(), ephemeral: ephemeral);
            else
                return di.RespondAsync(embed: new EmbedBuilder().WithOkColor().WithTitle(title).WithDescription(des).Build(), ephemeral: ephemeral);
        }

        public static Task SendConfirmAsync(this IDiscordInteraction di, BotLocalizer localizer, string locale,
            string resourceKey, bool isFollowerup = false, bool ephemeral = false, params object[] arguments)
            => di.SendConfirmAsync(localizer.Format(resourceKey, locale, arguments), isFollowerup, ephemeral);

        public static Task SendErrorAsync(this IDiscordInteraction di, string des, bool isFollowerup = false, bool ephemeral = true)
        {
            if (isFollowerup || di.HasResponded)
            {
                return di.FollowupAsync(embed: new EmbedBuilder().WithErrorColor().WithDescription(des).Build(), ephemeral: ephemeral);
            }

            return di.RespondAsync(embed: new EmbedBuilder().WithErrorColor().WithDescription(des).Build(), ephemeral: ephemeral);
        }

        public static Task SendErrorAsync(this IDiscordInteraction di, string title, string des, bool isFollowerup = false, bool ephemeral = true)
        {
            if (isFollowerup || di.HasResponded)
                return di.FollowupAsync(embed: new EmbedBuilder().WithErrorColor().WithTitle(title).WithDescription(des).Build(), ephemeral: ephemeral);
            else
                return di.RespondAsync(embed: new EmbedBuilder().WithErrorColor().WithTitle(title).WithDescription(des).Build(), ephemeral: ephemeral);
        }

        public static Task SendErrorAsync(this IDiscordInteraction di, BotLocalizer localizer, string locale,
            string resourceKey, bool isFollowerup = false, bool ephemeral = true, params object[] arguments)
            => di.SendErrorAsync(localizer.Format(resourceKey, locale, arguments), isFollowerup, ephemeral);

        public static IMessage DeleteAfter(this IUserMessage msg, int seconds)
        {
            Task.Run(async () =>
            {
                await Task.Delay(seconds * 1000).ConfigureAwait(false);
                try { await msg.DeleteAsync().ConfigureAwait(false); }
                catch { }
            });
            return msg;
        }

        public static IEnumerable<Type> LoadInteractionFrom(this IServiceCollection collection, Assembly assembly)
        {
            List<Type> addedTypes = new List<Type>();

            Type[] allTypes;
            try
            {
                allTypes = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                Console.WriteLine(ex.Message + "\n" + ex.Source);
                return Enumerable.Empty<Type>();
            }

            var services = new Queue<Type>(allTypes
                    .Where(x => x.GetInterfaces().Contains(typeof(IInteractionService))
                        && !x.GetTypeInfo().IsInterface && !x.GetTypeInfo().IsAbstract)
                    .ToArray());

            addedTypes.AddRange(services);

            var interfaces = new HashSet<Type>(allTypes
                    .Where(x => x.GetInterfaces().Contains(typeof(IInteractionService))
                        && x.GetTypeInfo().IsInterface));

            while (services.Count > 0)
            {
                var serviceType = services.Dequeue();

                if (collection.FirstOrDefault(x => x.ServiceType == serviceType) != null)
                    continue;

                var interfaceType = interfaces.FirstOrDefault(x => serviceType.GetInterfaces().Contains(x));
                if (interfaceType != null)
                {
                    addedTypes.Add(interfaceType);
                    collection.AddSingleton(interfaceType, serviceType);
                }
                else
                {
                    collection.AddSingleton(serviceType, serviceType);
                }
            }

            return addedTypes;
        }

        public static Task<IUserMessage> EmbedAsync(this IDiscordInteraction di, EmbedBuilder embed, string msg = "", bool ephemeral = false)
            => di.FollowupAsync(msg, embed: embed.Build(),
                options: new RequestOptions() { RetryMode = RetryMode.AlwaysRetry }, ephemeral: ephemeral);

        public static Task<IUserMessage> EmbedAsync(this IDiscordInteraction di, string msg = "", bool ephemeral = false)
           => di.FollowupAsync(embed: new EmbedBuilder().WithOkColor().WithDescription(msg).Build(),
               options: new RequestOptions { RetryMode = RetryMode.AlwaysRetry }, ephemeral: ephemeral);


        public static Task SendPaginatedConfirmAsync(this IInteractionContext ctx, int currentPage, Func<int, EmbedBuilder> pageFunc, int totalElements, int itemsPerPage, bool addPaginatedFooter = true, bool ephemeral = false, bool isFollowup = false)
            => ctx.SendPaginatedConfirmAsync(currentPage, (x) => Task.FromResult(pageFunc(x)), totalElements, itemsPerPage, addPaginatedFooter, ephemeral, isFollowup);

        public static Task SendPaginatedConfirmAsync(this IInteractionContext ctx, BotLocalizer localizer, string locale,
            int currentPage, Func<int, EmbedBuilder> pageFunc, int totalElements, int itemsPerPage,
            bool addPaginatedFooter = true, bool ephemeral = false, bool isFollowup = false)
            => ctx.SendPaginatedConfirmAsync(localizer, locale, currentPage, x => Task.FromResult(pageFunc(x)),
                totalElements, itemsPerPage, addPaginatedFooter, ephemeral, isFollowup);

        public static async Task SendPaginatedConfirmAsync(this IInteractionContext ctx, BotLocalizer localizer, string locale,
            int currentPage, Func<int, Task<EmbedBuilder>> pageFunc, int totalElements, int itemsPerPage,
            bool addPaginatedFooter = true, bool ephemeral = false, bool isFollowup = false)
        {
            var embed = await pageFunc(currentPage).ConfigureAwait(false);
            var lastPage = Math.Max(0, (totalElements - 1) / itemsPerPage);

            if (addPaginatedFooter)
                embed.AddPaginatedFooter(localizer, locale, currentPage, lastPage);

            string content = ephemeral ? localizer.Get("Pagination.EphemeralUnavailable", locale) : null;
            if (isFollowup || ctx.Interaction.HasResponded)
                await ctx.Interaction.FollowupAsync(content, embed: embed.Build(), ephemeral: ephemeral).ConfigureAwait(false);
            else
                await ctx.Interaction.RespondAsync(content, embed: embed.Build(), ephemeral: ephemeral).ConfigureAwait(false);

            if (ephemeral || lastPage == 0)
                return;

            var msg = await ctx.Interaction.GetOriginalResponseAsync().ConfigureAwait(false);
            try
            {
                await msg.AddReactionAsync(arrow_left).ConfigureAwait(false);
                await msg.AddReactionAsync(arrow_right).ConfigureAwait(false);
            }
            catch (Discord.Net.HttpException httpEx) when (httpEx.DiscordCode == DiscordErrorCode.MissingPermissions)
            {
                await ctx.Interaction.ModifyOriginalResponseAsync(action => action.Content = localizer.Get("Pagination.Unavailable", locale));
                return;
            }

            await Task.Delay(2000).ConfigureAwait(false);
            var lastPageChange = DateTime.MinValue;

            async Task ChangePage(SocketReaction reaction)
            {
                try
                {
                    if (reaction.UserId != ctx.User.Id || DateTime.UtcNow - lastPageChange < TimeSpan.FromSeconds(1))
                        return;

                    if (reaction.Emote.Name == arrow_left.Name && currentPage > 0)
                    {
                        lastPageChange = DateTime.UtcNow;
                        var toSend = await pageFunc(--currentPage).ConfigureAwait(false);
                        if (addPaginatedFooter)
                            toSend.AddPaginatedFooter(localizer, locale, currentPage, lastPage);
                        await msg.ModifyAsync(x => x.Embed = toSend.Build()).ConfigureAwait(false);
                    }
                    else if (reaction.Emote.Name == arrow_right.Name && currentPage < lastPage)
                    {
                        lastPageChange = DateTime.UtcNow;
                        var toSend = await pageFunc(++currentPage).ConfigureAwait(false);
                        if (addPaginatedFooter)
                            toSend.AddPaginatedFooter(localizer, locale, currentPage, lastPage);
                        await msg.ModifyAsync(x => x.Embed = toSend.Build()).ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    Log.Warn($"分頁切換已取消：{ex.GetType().Name}");
                }
            }

            using (msg.OnReaction((DiscordSocketClient)ctx.Client, ChangePage, ChangePage))
                await Task.Delay(30000).ConfigureAwait(false);

            try
            {
                if (msg.Channel is ITextChannel && ctx.Guild is SocketGuild guild && guild.CurrentUser.GuildPermissions.ManageMessages)
                    await msg.RemoveAllReactionsAsync().ConfigureAwait(false);
                else
                    await Task.WhenAll(msg.Reactions.Where(x => x.Value.IsMe).Select(x => msg.RemoveReactionAsync(x.Key, ctx.Client.CurrentUser)));
            }
            catch
            {
            }
        }

        public static async Task SendPaginatedConfirmAsync(this IInteractionContext ctx, int currentPage,
    Func<int, Task<EmbedBuilder>> pageFunc, int totalElements, int itemsPerPage, bool addPaginatedFooter = true, bool ephemeral = false, bool isFollowup = false)
        {
            var embed = await pageFunc(currentPage).ConfigureAwait(false);

            var lastPage = (totalElements - 1) / itemsPerPage;

            if (addPaginatedFooter)
                embed.AddPaginatedFooter(currentPage, lastPage);

            if (isFollowup) await ctx.Interaction.FollowupAsync(ephemeral ? "這是僅自己可見的回覆，無法換頁。\n如需換頁，請直接使用指令。" : null, embed: embed.Build(), ephemeral: ephemeral).ConfigureAwait(false);
            else await ctx.Interaction.RespondAsync(ephemeral ? "這是僅自己可見的回覆，無法換頁。\n如需換頁，請直接使用指令。" : null, embed: embed.Build(), ephemeral: ephemeral).ConfigureAwait(false);

            if (ephemeral)
                return;

            if (lastPage == 0)
                return;

            var msg = await ctx.Interaction.GetOriginalResponseAsync().ConfigureAwait(false);

            try
            {
                await msg.AddReactionAsync(arrow_left).ConfigureAwait(false);
                await msg.AddReactionAsync(arrow_right).ConfigureAwait(false);
            }
            catch (Discord.Net.HttpException httpEx) when (httpEx.DiscordCode == DiscordErrorCode.MissingPermissions)
            {
                await ctx.Interaction.ModifyOriginalResponseAsync((act) => act.Content = "無法換頁。如需換頁，請直接使用指令。");
                return;
            }

            await Task.Delay(2000).ConfigureAwait(false);

            var lastPageChange = DateTime.MinValue;

            async Task changePage(SocketReaction r)
            {
                try
                {
                    if (r.UserId != ctx.User.Id)
                        return;
                    if (DateTime.UtcNow - lastPageChange < TimeSpan.FromSeconds(1))
                        return;
                    if (r.Emote.Name == arrow_left.Name)
                    {
                        if (currentPage == 0)
                            return;
                        lastPageChange = DateTime.UtcNow;
                        var toSend = await pageFunc(--currentPage).ConfigureAwait(false);
                        if (addPaginatedFooter)
                            toSend.AddPaginatedFooter(currentPage, lastPage);
                        await msg.ModifyAsync(x => x.Embed = toSend.Build()).ConfigureAwait(false);
                    }
                    else if (r.Emote.Name == arrow_right.Name)
                    {
                        if (lastPage > currentPage)
                        {
                            lastPageChange = DateTime.UtcNow;
                            var toSend = await pageFunc(++currentPage).ConfigureAwait(false);
                            if (addPaginatedFooter)
                                toSend.AddPaginatedFooter(currentPage, lastPage);
                            await msg.ModifyAsync(x => x.Embed = toSend.Build()).ConfigureAwait(false);
                        }
                    }
                }
                catch (Exception)
                {
                    Console.WriteLine("作業已取消");
                    //ignored
                }
            }

            using (msg.OnReaction((DiscordSocketClient)ctx.Client, changePage, changePage))
            {
                await Task.Delay(30000).ConfigureAwait(false);
            }

            try
            {
                if (msg.Channel is ITextChannel && ((SocketGuild)ctx.Guild).CurrentUser.GuildPermissions.ManageMessages)
                {
                    await msg.RemoveAllReactionsAsync().ConfigureAwait(false);
                }
                else
                {
                    await Task.WhenAll(msg.Reactions.Where(x => x.Value.IsMe)
                        .Select(x => msg.RemoveReactionAsync(x.Key, ctx.Client.CurrentUser)));
                }
            }
            catch
            {
                // ignored
            }
        }

        public static EmbedBuilder AddPaginatedFooter(this EmbedBuilder embed, int curPage, int? lastPage)
        {
            if (lastPage != null)
                return embed.WithFooter(efb => efb.WithText($"{curPage + 1} / {lastPage + 1}"));
            else
                return embed.WithFooter(efb => efb.WithText(curPage.ToString()));
        }

        public static EmbedBuilder AddPaginatedFooter(this EmbedBuilder embed, BotLocalizer localizer, string locale, int curPage, int? lastPage)
        {
            string footer = lastPage != null
                ? localizer.Format("Pagination.Footer", locale, curPage + 1, lastPage + 1)
                : (curPage + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
            return embed.WithFooter(footer);
        }

        public static ReactionEventWrapper OnReaction(this IUserMessage msg, DiscordSocketClient client, Func<SocketReaction, Task> reactionAdded, Func<SocketReaction, Task> reactionRemoved = null)
        {
            if (reactionRemoved == null)
                reactionRemoved = _ => Task.CompletedTask;

            var wrap = new ReactionEventWrapper(client, msg);
            wrap.OnReactionAdded += (r) => { var _ = Task.Run(() => reactionAdded(r)); };
            wrap.OnReactionRemoved += (r) => { var _ = Task.Run(() => reactionRemoved(r)); };
            return wrap;
        }
    }
}
