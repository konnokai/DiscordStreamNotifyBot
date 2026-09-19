namespace DiscordStreamNotifyBot
{
    /// <summary>
    /// 判斷 Discord 例外是否代表「這個通知目標再也不會成功」。這類失敗留在匯流排只會被 XAUTOCLAIM
    /// 無限重投，因此各服務改為略過該目標、清除對應的通知設定，不做重試。
    /// <para>
    /// 只認明確的 Discord 錯誤碼：權限不足與目標（頻道／伺服器）已不存在。
    /// 5xx、timeout、rate limit 等暫時性錯誤不在此列，維持原本的重試行為。
    /// </para>
    /// </summary>
    internal static class NotificationTargetFailure
    {
        internal static bool IsPermanent(Exception exception)
            => exception is Discord.Net.HttpException httpEx && IsPermanentCode(httpEx.DiscordCode);

        internal static bool IsPermanentCode(DiscordErrorCode? code)
            => code is DiscordErrorCode.MissingPermissions
                or DiscordErrorCode.InsufficientPermissions
                or DiscordErrorCode.UnknownChannel
                or DiscordErrorCode.UnknownGuild;

        /// <summary>伺服器已不存在時應清除整個伺服器的設定，其餘永久失敗只清除該目的地頻道。</summary>
        internal static bool IsUnknownGuild(Discord.Net.HttpException httpEx)
            => httpEx.DiscordCode == DiscordErrorCode.UnknownGuild;
    }
}
