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
}
