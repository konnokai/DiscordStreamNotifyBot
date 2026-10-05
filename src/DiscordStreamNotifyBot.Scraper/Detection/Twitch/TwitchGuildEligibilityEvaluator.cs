using DiscordStreamNotifyBot.DataBase.Table;
using DiscordStreamNotifyBot.Shared;
using DiscordStreamNotifyBot.Shared.Messages;
using System.Collections.Concurrent;

using Bot = DiscordStreamNotifyBot.Shared.BotState;

namespace DiscordStreamNotifyBot.Scraper.Detection.Twitch
{
    internal enum TwitchGuildEligibilityStatus
    {
        /// <summary>Guild 符合人數門檻、位於官方白名單，或 spider 不綁定特定 guild。</summary>
        Eligible,
        /// <summary>Guild 存在於有效快照，但成員數未達門檻。</summary>
        Ineligible,
        /// <summary>經過等待時間且後續新快照仍找不到 guild，可確認 guild 已不在該 shard。</summary>
        MissingConfirmed,
        /// <summary>首次發現 guild 缺失，尚待下一代快照與確認時間。</summary>
        PendingSnapshot,
        /// <summary>缺少 shard 數、快照不存在、內容無效或已過期。</summary>
        SnapshotUnavailable,
        /// <summary>負責該 guild 的 Notifier shard 沒有有效心跳，不能信任其快照。</summary>
        NotifierUnavailable
    }

    /// <summary>
    /// 透過 Coordinator 公告與 Notifier guild 快照判斷 Twitch spider 的 guild 是否仍符合保留資格。
    /// 評估採保守策略：任何基礎資料不完整時只延後清理，不回傳可刪除狀態。
    /// </summary>
    internal sealed class TwitchGuildEligibilityEvaluator
    {
        // 快照更新週期遠短於此值；超過上限視為 owner shard 狀態不可信。
        private static readonly TimeSpan SnapshotMaxAge = TimeSpan.FromMinutes(20);
        // 首次發現 guild 缺失後至少等待此時間，且需出現更新的快照，才可確認 guild 已不在該 shard。
        private static readonly TimeSpan MissingConfirmationDelay = TimeSpan.FromMinutes(15);
        private readonly ClusterService _cluster;
        private readonly TimeProvider _timeProvider;
        // 記錄首次觀察到 guild 缺失的快照世代，僅供本程序內的二階段確認。
        private readonly ConcurrentDictionary<ulong, MissingGuildObservation> _missingGuildGenerations = new();

        public TwitchGuildEligibilityEvaluator(ClusterService cluster)
        {
            _cluster = cluster;
            _timeProvider = TimeProvider.System;
        }

        /// <summary>
        /// 評估 spider 所屬 guild。只有明確的 <see cref="TwitchGuildEligibilityStatus.Ineligible"/> 或
        /// <see cref="TwitchGuildEligibilityStatus.MissingConfirmed"/> 才能作為後續移除 spider 的依據。
        /// </summary>
        public async Task<TwitchGuildEligibilityStatus> EvaluateAsync(TwitchSpider spider)
        {
            // 不綁 guild 的舊資料與官方 guild 不受一般成員數門檻限制。
            if (spider.GuildId == 0 || await IsOfficialGuildAsync(spider.GuildId))
            {
                _missingGuildGenerations.TryRemove(spider.GuildId, out _);
                return TwitchGuildEligibilityStatus.Eligible;
            }

            int totalShards;
            try
            {
                int? announcedTotalShards = await _cluster.GetTotalShardsAsync();
                if (announcedTotalShards is null or <= 0)
                    return TwitchGuildEligibilityStatus.SnapshotUnavailable;

                totalShards = announcedTotalShards.Value;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), $"讀取 Twitch spider guild {spider.GuildId} 的 TOTAL_SHARDS 失敗");
                return TwitchGuildEligibilityStatus.SnapshotUnavailable;
            }

            // Discord snowflake 可直接推導負責此 guild 的 shard，避免掃描所有 Notifier 快照。
            int ownerShard = (int)((spider.GuildId >> 22) % (ulong)totalShards);
            try
            {
                // Owner shard 離線時，其最後快照即使尚未過期也可能已不再代表目前 guild 狀態。
                if (!await _cluster.IsHeartbeatAliveAsync("notifier", $"shard{ownerShard}"))
                    return TwitchGuildEligibilityStatus.NotifierUnavailable;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), $"確認 Twitch spider guild {spider.GuildId} 的 owner shard 心跳失敗");
                return TwitchGuildEligibilityStatus.NotifierUnavailable;
            }

            GuildSnapshotEnvelope envelope;
            try
            {
                RedisValue value = await Bot.RedisDb.HashGetAsync(RedisChannels.SharedState.GuildSnapshotHash, ownerShard);
                if (value.IsNullOrEmpty)
                    return TwitchGuildEligibilityStatus.SnapshotUnavailable;

                envelope = JsonConvert.DeserializeObject<GuildSnapshotEnvelope>(value!);
                // 必須同時驗證 shard 身分、Discord 連線狀態、內容與新鮮度，才可用於破壞性清理。
                if (envelope == null || envelope.ShardId != ownerShard || !envelope.IsConnected || envelope.Guilds == null ||
                    envelope.UpdatedAtUtc == default || _timeProvider.GetUtcNow().UtcDateTime - envelope.UpdatedAtUtc > SnapshotMaxAge)
                    return TwitchGuildEligibilityStatus.SnapshotUnavailable;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), $"解析 Twitch spider guild {spider.GuildId} 的 owner shard 快照失敗");
                return TwitchGuildEligibilityStatus.SnapshotUnavailable;
            }

            var guild = envelope.Guilds.FirstOrDefault(x => x.Id == spider.GuildId);
            if (guild != null)
            {
                _missingGuildGenerations.TryRemove(spider.GuildId, out _);
                return guild.MemberCount >= 200
                    ? TwitchGuildEligibilityStatus.Eligible
                    : TwitchGuildEligibilityStatus.Ineligible;
            }

            return ObserveMissingGuild(spider.GuildId, ownerShard, envelope.UpdatedAtUtc);
        }

        /// <summary>
        /// Guild 不在 owner shard 快照時的二階段確認：首次（或 owner shard 改變）只記錄觀察，
        /// 之後需出現更新的快照且超過確認等待時間，才回傳 <see cref="TwitchGuildEligibilityStatus.MissingConfirmed"/>。
        /// </summary>
        private TwitchGuildEligibilityStatus ObserveMissingGuild(ulong guildId, int ownerShard, DateTime snapshotUpdatedAtUtc)
        {
            DateTime nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
            if (!_missingGuildGenerations.TryGetValue(guildId, out var previous) || previous.ShardId != ownerShard)
            {
                _missingGuildGenerations[guildId] = new MissingGuildObservation(ownerShard, snapshotUpdatedAtUtc, nowUtc);
                return TwitchGuildEligibilityStatus.PendingSnapshot;
            }

            if (snapshotUpdatedAtUtc <= previous.SnapshotUpdatedAtUtc ||
                nowUtc - previous.FirstObservedAtUtc < MissingConfirmationDelay)
                return TwitchGuildEligibilityStatus.PendingSnapshot;

            return TwitchGuildEligibilityStatus.MissingConfirmed;
        }

        private static async Task<bool> IsOfficialGuildAsync(ulong guildId)
        {
            try
            {
                return await Bot.RedisDb.SetContainsAsync(RedisChannels.SharedState.OfficialGuildList, guildId);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), $"確認 Twitch spider guild {guildId} 官方白名單失敗");
                // Redis 無法讀取時退回程序內建名單，避免官方 guild 因基礎設施故障被誤刪。
                return Utility.OfficialGuildContains(guildId);
            }
        }

        private sealed record MissingGuildObservation(int ShardId, DateTime SnapshotUpdatedAtUtc, DateTime FirstObservedAtUtc);
    }
}
