using DiscordStreamNotifyBot.Shared;
using DiscordStreamNotifyBot.Shared.Messages;
using Dorssel.Utilities.Generic;

namespace DiscordStreamNotifyBot.Scraper.Detection.Twitch.Debounce
{
    // https://blog.darkthread.net/blog/dotnet-debounce/
    // https://github.com/dorssel/dotnet-debounce
    internal sealed class DebounceChannelUpdateMessage : IDisposable
    {
        private readonly Debouncer<TwitchChannelUpdateInfo> _debouncer;
        private readonly Func<string, string, string, IReadOnlyCollection<TwitchChannelUpdateInfo>, Task> _publishAsync;
        private readonly CancellationTokenRegistration _cancellationRegistration;
        private readonly string _twitchUserName, _twitchUserLogin, _twitchUserId;

        public DebounceChannelUpdateMessage(TwitchDetectionService twitchService, string twitchUserName, string twitchUserLogin, string twitchUserId)
        {
            _twitchUserName = twitchUserName;
            _twitchUserLogin = twitchUserLogin;
            _twitchUserId = twitchUserId;
            _publishAsync = twitchService.PublishChannelUpdateAsync;

            _debouncer = new(TimeProvider.System)
            {
                DebounceWindow = TimeSpan.FromMinutes(1),
                DebounceTimeout = TimeSpan.FromMinutes(3),
            };
            _debouncer.Debounced += _debouncer_Debounced;
            CancellationToken cancellationToken = GracefulShutdown.Token;
            if (cancellationToken.CanBeCanceled)
                _cancellationRegistration = cancellationToken.Register(CancelPending);
        }

        private void _debouncer_Debounced(object sender, DebouncedEventArgs<TwitchChannelUpdateInfo> e)
        {
            try
            {
                Log.Info($"{_twitchUserLogin} 發送頻道更新通知（Debouncer 觸發數量：{e.Count}）");

                // publish DTO 至匯流排，由消費端（Notifier）重建 embed 發送
                var updates = e.TriggerData.ToArray();
                // 發布委派（TwitchDetectionService.PublishChannelUpdateAsync）自行攔截並記錄例外。
                _ = Task.Run(() => _publishAsync(_twitchUserId, _twitchUserName, _twitchUserLogin, updates));
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), $"{_twitchUserLogin} 訊息去抖動失敗");
            }
        }

        public void AddUpdate(TwitchChannelUpdateInfo update)
        {
            if (Volatile.Read(ref _isDisposed) != 0)
                throw new ObjectDisposedException(nameof(DebounceChannelUpdateMessage));

            Log.Debug($"DebounceChannelUpdateMessage ({_twitchUserLogin}): {JsonConvert.SerializeObject(update)}");

            _debouncer.Trigger(update);
        }

        private void CancelPending()
        {
            _debouncer.Reset();
        }

        private int _isDisposed;
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _isDisposed, 1) != 0)
                return;

            _cancellationRegistration.Dispose();
            _debouncer.Debounced -= _debouncer_Debounced;
            _debouncer.Dispose();
        }
    }
}
