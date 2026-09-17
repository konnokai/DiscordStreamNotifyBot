namespace DiscordStreamNotifyBot
{
    /// <summary>
    /// 單一通知目標的發送結果。用於決定是否把該目標標記為完成：可重試的失敗不標記，
    /// 讓佇列進度保存與 XAUTOCLAIM 補送接手。Prometheus 指標層已移除，此分類仍由發送流程使用。
    /// </summary>
    internal enum NotificationDeliveryResult
    {
        Sent,
        Disabled,
        MissingGuild,
        MissingChannel,
        MissingPermission,
        Discord5xx,
        Timeout,
        AuthorizationFailure,
        UnknownError
    }
}
