using System.Runtime.InteropServices;

namespace DiscordStreamNotifyBot.Shared
{
    /// <summary>
    /// 統一的優雅關閉訊號處理（計畫 §11-1）。
    /// <para>
    /// 同時攔截 SIGINT（Ctrl+C）與 <b>SIGTERM</b>（<c>docker stop</c> 預設送出），
    /// 取消回傳的 <see cref="Token"/>，讓主迴圈得以執行清理後結束；
    /// 取代僅攔 SIGINT 的 <c>Console.CancelKeyPress</c> 寫法。
    /// </para>
    /// </summary>
    public static class GracefulShutdown
    {
        private static readonly CancellationTokenSource _cts = new();
        private static int _initialized;

        /// <summary>於程序關閉訊號時被取消的 token。</summary>
        public static CancellationToken Token => _cts.Token;

        /// <summary>註冊訊號處理（可重複呼叫，僅第一次生效）。</summary>
        public static void Init()
        {
            if (Interlocked.Exchange(ref _initialized, 1) != 0)
                return;

            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true; // 不要立即終止，交由主迴圈優雅結束
                RequestShutdown();
            };

            AppDomain.CurrentDomain.ProcessExit += (_, _) => RequestShutdown();

            // SIGTERM（容器停止）/ SIGINT：.NET 6+ 的 PosixSignalRegistration（跨平台）
            PosixSignalRegistration.Create(PosixSignal.SIGTERM, ctx => { ctx.Cancel = true; RequestShutdown(); });
            PosixSignalRegistration.Create(PosixSignal.SIGINT, ctx => { ctx.Cancel = true; RequestShutdown(); });
        }

        private static void RequestShutdown()
        {
            if (!_cts.IsCancellationRequested)
            {
                Log.Info("收到關閉訊號，開始優雅關閉…");
                try { _cts.Cancel(); } catch { }
            }
        }
    }
}
