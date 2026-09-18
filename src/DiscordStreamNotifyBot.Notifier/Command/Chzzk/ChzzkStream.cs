using Discord.Commands;
using DiscordStreamNotifyBot.Command.Attribute;
using DiscordStreamNotifyBot.DataBase;
using DiscordStreamNotifyBot.Shared;
using DiscordStreamNotifyBot.SharedService.Chzzk;

namespace DiscordStreamNotifyBot.Command.Chzzk
{
    /// <summary>
    /// CHZZK 錄影的 Bot 擁有者前綴指令（私訊、<c>s!</c> 前綴）。
    /// 設定與發布邏輯共用 <see cref="ChzzkRecordService"/>，與爬蟲新增通知的按鈕規則一致。
    /// </summary>
    public class ChzzkStream : TopLevelModule, ICommandService
    {
        private readonly MainDbService _dbService;
        private readonly ChzzkRecordService _recordService;

        public ChzzkStream(MainDbService dbService, ChzzkRecordService recordService)
        {
            _dbService = dbService;
            _recordService = recordService;
        }

        [RequireContext(ContextType.DM)]
        [Command("ChzzkRecord")]
        [Summary("馬上錄影")]
        [Alias("CRR")]
        [CommandExample("https://chzzk.naver.com/live/64d76089fba26b180d9c9e48a32600d9")]
        [RequireOwner]
        public async Task RecordAsync(string channel)
        {
            await Context.Channel.TriggerTypingAsync();

            if (!ChzzkUrlParser.TryParseChannelId(channel, out string channelId))
            {
                await Context.Channel.SendErrorAsync("頻道格式驗證失敗，請輸入 CHZZK 頻道網址或 32 位頻道 ID");
                return;
            }

            var result = await _recordService.RequestRecordAsync(channelId, GracefulShutdown.Token);
            switch (result.Code)
            {
                case "record.requested":
                    await Context.Channel.SendConfirmAsync("已送出錄影請求",
                        $"頻道: `{channelId}`\n場次: `{result.Arguments.Value<string>("streamKey")}`\n\n" +
                        "錄影工具尚未確認開始錄影，請確認錄影端是否在線。");
                    return;
                case "record.not-live":
                    await Context.Channel.SendErrorAsync("目前非開台狀態，未發送錄影請求");
                    return;
                case "record.stream-key-unavailable":
                    await Context.Channel.SendErrorAsync("無法建立場次鍵，未發送錄影請求");
                    return;
                case "record.record-tool-offline":
                    await Context.Channel.SendErrorAsync("Redis 訂閱頻道不存在，請先啟動錄影工具");
                    return;
                case "record.status-unavailable":
                    await Context.Channel.SendErrorAsync("無法取得直播狀態，請確認頻道存在或稍後再試");
                    return;
                default:
                    await Context.Channel.SendErrorAsync("錄影請求發布失敗");
                    return;
            }
        }

        [RequireContext(ContextType.DM)]
        [Command("ChzzkAutoRecord")]
        [Summary("開啟或關閉頻道自動錄影")]
        [Alias("CCAR")]
        [CommandExample("https://chzzk.naver.com/64d76089fba26b180d9c9e48a32600d9 on")]
        [RequireOwner]
        public async Task AutoRecordAsync(string channel, string setting)
        {
            await Context.Channel.TriggerTypingAsync();

            if (!ChzzkUrlParser.TryParseChannelId(channel, out string channelId))
            {
                await Context.Channel.SendErrorAsync("頻道格式驗證失敗，請輸入 CHZZK 頻道網址或 32 位頻道 ID");
                return;
            }

            bool enabled;
            if (string.Equals(setting, "on", StringComparison.OrdinalIgnoreCase))
                enabled = true;
            else if (string.Equals(setting, "off", StringComparison.OrdinalIgnoreCase))
                enabled = false;
            else
            {
                await Context.Channel.SendErrorAsync("設定值需為 on 或 off");
                return;
            }

            var result = await _recordService.SetAutoRecordAsync(channelId, enabled, GracefulShutdown.Token);
            if (result.Code == "record.not-configured")
            {
                await Context.Channel.SendErrorAsync(
                    $"找不到 `{channelId}` 的爬蟲；自動錄影設定要求爬蟲存在，請先新增爬蟲");
                return;
            }

            await Context.Channel.SendConfirmAsync($"已{(enabled ? "開啟" : "關閉")}自動錄影",
                $"頻道: {result.Arguments.Value<string>("sourceName") ?? channelId} (`{channelId}`)\n\n" +
                "只對之後偵測到的新場次委派錄影；已發過開台通知的場次不會補錄。" +
                (enabled ? "\n要補錄目前場次請使用 `s!ChzzkRecord <頻道網址>`。" : ""));
        }

        [RequireContext(ContextType.DM)]
        [Command("ChzzkRecordList")]
        [Summary("列出已開啟自動錄影的頻道")]
        [Alias("CCRL")]
        [RequireOwner]
        public async Task RecordListAsync()
        {
            await Context.Channel.TriggerTypingAsync();

            using var db = _dbService.GetDbContext();
            var recordList = await db.ChzzkSpider.AsNoTracking()
                .Where(x => x.IsRecord)
                .OrderBy(x => x.ChannelName)
                .ToListAsync();

            if (recordList.Count == 0)
            {
                await Context.Channel.SendConfirmAsync("目前沒有開啟自動錄影的 CHZZK 頻道");
                return;
            }

            var lines = recordList.Select(spider =>
                $"{Format.Url(string.IsNullOrEmpty(spider.ChannelName) ? spider.ChannelId : spider.ChannelName, ChzzkUrls.Channel(spider.ChannelId))} `{spider.ChannelId}`")
                .ToList();

            // 只表示自動錄影設定，不代表目前有錄影程序正在執行。
            await Context.SendPaginatedConfirmAsync(0, page => new EmbedBuilder()
                .WithOkColor()
                .WithTitle("CHZZK 自動錄影清單")
                .WithDescription(string.Join('\n', lines.Skip(page * 20).Take(20)))
                .WithFooter($"{Math.Min(lines.Count, (page + 1) * 20)} / {lines.Count}個頻道"),
                lines.Count, 20, false);
        }
    }
}
