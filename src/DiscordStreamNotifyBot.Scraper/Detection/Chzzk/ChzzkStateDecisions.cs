using DiscordStreamNotifyBot.DataBase.Table;

namespace DiscordStreamNotifyBot.Scraper.Detection.Chzzk
{
    /// <summary>單次 CHZZK 觀察後的狀態機動作（計畫 §4 生命週期契約）。</summary>
    internal enum ChzzkPollAction
    {
        /// <summary>資料不可信（未知 status、缺必要欄位、openDate 無效）：本輪不更新狀態、不發通知。</summary>
        Unknown,

        /// <summary>首次有效 CLOSE：只建立離線基線，不對上一場補發關台。</summary>
        BaselineOffline,

        /// <summary>舊場次之後的新場次：建立場次並發布開台。</summary>
        TrackNewStream,

        /// <summary>目前場次仍在進行／待確認時出現新鍵：舊場標示 Superseded，建立新場次並發布開台。</summary>
        SupersedeAndTrack,

        /// <summary>同鍵 OPEN：更新快照，不重發開台。</summary>
        RefreshObserved,

        /// <summary>同鍵 OPEN 且正在關台確認：取消確認，不重發開台。</summary>
        CancelPendingClose,

        /// <summary>已知場次收到同鍵 CLOSE：進入關台確認。</summary>
        StartPendingClose,

        /// <summary>關台確認已達等待時間且再次收到同鍵 CLOSE：定案並發布關台。</summary>
        ConfirmClose,

        /// <summary>不同鍵的 CLOSE、已關台／已取代場次的重複 CLOSE：不得改動目前場次。</summary>
        Ignore
    }

    internal sealed record ChzzkPollFacts(
        bool IsOpen,
        bool HasValidStreamKey,
        string StreamKey,
        bool IsInitialized,
        string CurrentStreamKey,
        ChzzkStreamStatus? CurrentStatus,
        DateTime? PendingCloseSinceUtc,
        DateTime NowUtc);

    /// <summary>
    /// CHZZK 場次生命週期決策（純函式，可單元測試）。
    /// <para>
    /// 契約重點：相同鍵不重發、新 openDate 為新場、舊場 CLOSE 不關閉新場、未知資料不轉離線、
    /// 關台需延遲後重新確認且重複 CLOSE 不重設等待起點。
    /// </para>
    /// </summary>
    internal static class ChzzkPollPolicy
    {
        /// <summary>關台確認等待時間；使用者已決定為 3 分鐘。</summary>
        internal static readonly TimeSpan CloseConfirmationDelay = TimeSpan.FromMinutes(3);

        public static ChzzkPollAction Decide(ChzzkPollFacts facts)
        {
            if (facts.IsOpen)
            {
                if (!facts.HasValidStreamKey)
                    return ChzzkPollAction.Unknown;
                if (string.IsNullOrEmpty(facts.CurrentStreamKey))
                    return ChzzkPollAction.TrackNewStream;
                if (!string.Equals(facts.CurrentStreamKey, facts.StreamKey, StringComparison.Ordinal))
                    return ChzzkPollAction.SupersedeAndTrack;

                return facts.CurrentStatus == ChzzkStreamStatus.PendingClose
                    ? ChzzkPollAction.CancelPendingClose
                    : ChzzkPollAction.RefreshObserved;
            }

            // CLOSE：尚未初始化時建立離線基線，不需要場次鍵。
            if (!facts.IsInitialized)
                return ChzzkPollAction.BaselineOffline;
            if (string.IsNullOrEmpty(facts.CurrentStreamKey) ||
                !string.Equals(facts.CurrentStreamKey, facts.StreamKey, StringComparison.Ordinal))
                return ChzzkPollAction.Ignore;

            switch (facts.CurrentStatus)
            {
                case ChzzkStreamStatus.Open:
                    return ChzzkPollAction.StartPendingClose;
                case ChzzkStreamStatus.PendingClose:
                    return facts.PendingCloseSinceUtc is { } since &&
                        facts.NowUtc - since >= CloseConfirmationDelay
                            ? ChzzkPollAction.ConfirmClose
                            : ChzzkPollAction.Ignore;
                default:
                    return ChzzkPollAction.Ignore;
            }
        }
    }
}
