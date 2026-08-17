namespace DiscordStreamNotifyBot.Shared
{
    /// <summary>
    /// 固定間隔背景輪詢工具（計畫 §12.1）：以 <see cref="PeriodicTimer"/> 取代 <c>System.Threading.Timer</c>。
    /// <para>
    /// <b>await 友善、天然無重入</b>：每次 <paramref name="action"/> 完成後才等下一個 tick，前後不重疊，
    /// 省去手寫的 <c>isRunning</c> 重入旗標；支援 <see cref="CancellationToken"/>，關閉時迴圈會自然結束，可處理 SIGTERM。
    /// </para>
    /// </summary>
    public static class PeriodicRunner
    {
        /// <summary>
        /// 啟動背景輪詢：等待 <paramref name="dueTime"/> 後首次執行，之後每 <paramref name="interval"/> 執行一次，
        /// 直到 <paramref name="token"/> 取消。單次例外僅記錄，不中斷迴圈。
        /// </summary>
        /// <returns>背景 Task（通常不需保留參考；生命週期由 <paramref name="token"/> 控制）。</returns>
        public static Task RunAsync(string name, TimeSpan dueTime, TimeSpan interval, Func<Task> action, CancellationToken token)
        {
            return Task.Run(
                () => RunCoreAsync(name, dueTime, interval, action, TimeProvider.System, token),
                token);
        }

        internal static async Task RunCoreAsync(
            string name,
            TimeSpan dueTime,
            TimeSpan interval,
            Func<Task> action,
            TimeProvider timeProvider,
            CancellationToken token)
        {
            try
            {
                if (dueTime > TimeSpan.Zero)
                    await Task.Delay(dueTime, timeProvider, token).ConfigureAwait(false);

                using var timer = new PeriodicTimer(interval, timeProvider);
                do
                {
                    try
                    {
                        await action().ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (token.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.Demystify(), $"PeriodicRunner[{name}]");
                    }
                }
                while (await timer.WaitForNextTickAsync(token).ConfigureAwait(false));
            }
            catch (OperationCanceledException)
            {
                // 關閉中，正常結束
            }
        }
    }
}
