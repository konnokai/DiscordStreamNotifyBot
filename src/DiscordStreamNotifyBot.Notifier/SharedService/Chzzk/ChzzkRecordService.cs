using DiscordStreamNotifyBot.DataBase;
using DiscordStreamNotifyBot.DataBase.Table;
using DiscordStreamNotifyBot.HttpClients.Chzzk;
using DiscordStreamNotifyBot.HttpClients.Chzzk.Model;
using DiscordStreamNotifyBot.Shared;
using DiscordStreamNotifyBot.Shared.Messages;
using DiscordStreamNotifyBot.SharedService.Chzzk;
using Newtonsoft.Json.Linq;

namespace DiscordStreamNotifyBot.SharedService.Chzzk
{
    /// <summary>
    /// CHZZK 錄影相關的最小共用邏輯：立即錄影的前置檢查與發布、自動錄影設定的讀寫。
    /// Bot 擁有者前綴指令與爬蟲新增通知按鈕共用同一份規則，避免兩邊設定語意分歧。
    /// <para>不含停止單場錄影、全域開關與補送機制；自動委派由 Scraper 負責。</para>
    /// </summary>
    public sealed class ChzzkRecordService
    {
        private readonly MainDbService _dbService;
        private readonly ChzzkClient _chzzkClient;

        public ChzzkRecordService(MainDbService dbService, ChzzkClient chzzkClient)
        {
            _dbService = dbService;
            _chzzkClient = chzzkClient;
        }

        /// <summary>
        /// 明確設定自動錄影的目標值（不是反轉）。爬蟲必須存在，不隱含新增爬蟲或改變其 guild 歸屬；
        /// 關閉時不停止已經執行中的錄影工作。
        /// </summary>
        public async Task<AdminSettingsMutationResult> SetAutoRecordAsync(
            string channelId, bool enabled, CancellationToken cancellationToken)
        {
            using var db = _dbService.GetDbContext();
            var spider = await db.ChzzkSpider.SingleOrDefaultAsync(x => x.ChannelId == channelId, cancellationToken);
            if (spider == null)
                return AdminSettingsMutationResult.Rejected("record.not-configured");

            return await ApplyAutoRecordAsync(db, spider, enabled, cancellationToken);
        }

        /// <summary>
        /// 依目前狀態反轉自動錄影（爬蟲管理按鈕語意，對齊 Twitch／TwitCasting 的切換按鈕）；
        /// 與 <see cref="SetAutoRecordAsync"/> 共用同一段讀取與儲存邏輯。
        /// </summary>
        public async Task<AdminSettingsMutationResult> ToggleAutoRecordAsync(
            string channelId, CancellationToken cancellationToken)
        {
            using var db = _dbService.GetDbContext();
            var spider = await db.ChzzkSpider.SingleOrDefaultAsync(x => x.ChannelId == channelId, cancellationToken);
            if (spider == null)
                return AdminSettingsMutationResult.Rejected("record.not-configured");

            return await ApplyAutoRecordAsync(db, spider, !spider.IsRecord, cancellationToken);
        }

        private static async Task<AdminSettingsMutationResult> ApplyAutoRecordAsync(
            MainDbContext db, ChzzkSpider spider, bool enabled, CancellationToken cancellationToken)
        {
            spider.IsRecord = enabled;
            await db.SaveChangesAsync(cancellationToken);
            Log.Info($"CHZZK 自動錄影已{(enabled ? "開啟" : "關閉")}：{spider.ChannelId}");
            return AdminSettingsMutationResult.Applied(
                enabled ? "record.enabled" : "record.disabled",
                new JObject
                {
                    ["sourceId"] = spider.ChannelId,
                    ["sourceName"] = spider.ChannelName,
                    ["enabled"] = enabled
                });
        }

        /// <summary>
        /// 立即錄影：查詢目前直播，必須 OPEN 且可建立場次鍵才發布；不要求已有爬蟲，也不修改自動錄影設定。
        /// </summary>
        public async Task<AdminSettingsMutationResult> RequestRecordAsync(string channelId, CancellationToken cancellationToken)
        {
            var status = await _chzzkClient.GetLiveStatusAsync(channelId, cancellationToken).ConfigureAwait(false);
            return await PublishRecordRequestAsync(channelId, status, PublishAsync).ConfigureAwait(false);
        }

        /// <summary>
        /// 立即錄影的判斷與發布；以 API 結果與發布委派為輸入，讓測試不需 HTTP 與 Redis。
        /// 失敗原因以 code 回傳，由呼叫端轉成可理解的訊息；有訂閱者也只代表請求已送出，不宣稱正在錄影。
        /// </summary>
        internal static async Task<AdminSettingsMutationResult> PublishRecordRequestAsync(
            string channelId,
            ChzzkLiveStatusResult status,
            Func<string, string, Task<long>> publishRecordAsync)
        {
            if (!status.IsSuccess)
                return AdminSettingsMutationResult.Rejected("record.status-unavailable");
            if (ChzzkClient.TryGetKnownStatus(status) != ChzzkLiveStatusValues.Open)
                return AdminSettingsMutationResult.Rejected("record.not-live");
            if (!ChzzkStreamIdentity.TryCreate(channelId, status.Status.OpenDate, out string streamKey))
                return AdminSettingsMutationResult.Rejected("record.stream-key-unavailable");

            long receiverCount;
            try
            {
                receiverCount = await publishRecordAsync(channelId, streamKey);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), $"CHZZK 立即錄影請求發布失敗：{channelId}");
                return AdminSettingsMutationResult.Rejected("record.publish-failed");
            }

            if (receiverCount <= 0)
                return AdminSettingsMutationResult.Rejected("record.record-tool-offline");

            Log.Info($"已發送 CHZZK 立即錄影請求：{streamKey}");
            return AdminSettingsMutationResult.Applied("record.requested", new JObject
            {
                ["sourceId"] = channelId,
                ["streamKey"] = streamKey
            });
        }

        private static Task<long> PublishAsync(string channelId, string streamKey)
            => Bot.RedisSub == null
                ? Task.FromResult(0L)
                : ChzzkRecordBus.PublishAsync(Bot.RedisSub, channelId, streamKey);
    }
}
