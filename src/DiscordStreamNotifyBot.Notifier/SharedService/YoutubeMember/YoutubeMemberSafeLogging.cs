namespace DiscordStreamNotifyBot.SharedService.YoutubeMember
{
    /// <summary>OAuth/Google SDK 例外可能夾帶 response body；此處刻意只輸出操作名稱與例外型別。</summary>
    internal static class YoutubeMemberSafeLogging
    {
        public static string DescribeFailure(string operation, Exception exception)
            => $"{operation} 失敗: {exception?.GetType().Name ?? "Unknown"}";
    }
}
