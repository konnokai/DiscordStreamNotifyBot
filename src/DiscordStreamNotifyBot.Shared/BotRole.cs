namespace DiscordStreamNotifyBot.Shared
{
    /// <summary>
    /// 叢集中的程序角色。對應 <c>bot_config.json</c> 的 <c>Role</c> 欄位 / 環境變數 <c>ROLE</c>。
    /// </summary>
    public enum BotRole
    {
        /// <summary>爬蟲層：所有輪詢偵測、錄影 IPC、PubSub 維護；不連 Discord gateway。</summary>
        Scraper,

        /// <summary>通知層 (shard)：連 Discord、指令系統、依 shard 歸屬發送通知。</summary>
        Notifier,

        /// <summary>主控層：心跳監控、leader 選舉、shard 租約分配、叢集狀態回報。</summary>
        Coordinator
    }
}
