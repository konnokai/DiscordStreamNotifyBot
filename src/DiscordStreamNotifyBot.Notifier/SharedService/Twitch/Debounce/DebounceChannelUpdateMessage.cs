using DiscordStreamNotifyBot.Interaction;
using Dorssel.Utilities;
using System.Collections.Concurrent;
using static DiscordStreamNotifyBot.SharedService.Twitch.TwitchService;

namespace DiscordStreamNotifyBot.SharedService.Twitch.Debounce
{
    // https://blog.darkthread.net/blog/dotnet-debounce/
    // https://github.com/dorssel/dotnet-debounce
    internal class DebounceChannelUpdateMessage
    {
        private readonly Debouncer _debouncer;
        private readonly TwitchService _twitchService;
        private readonly string _twitchUserName, _twitchUserLogin, _twitchUserId;
        private readonly ConcurrentQueue<string> messageQueue = new();

        public DebounceChannelUpdateMessage(TwitchService twitchService, string twitchUserName, string twitchUserLogin, string twitchUserId)
        {
            _twitchService = twitchService;
            _twitchUserName = twitchUserName;
            _twitchUserLogin = twitchUserLogin;
            _twitchUserId = twitchUserId;

            _debouncer = new()
            {
                DebounceWindow = TimeSpan.FromMinutes(1),
                DebounceTimeout = TimeSpan.FromMinutes(3),
            };
            _debouncer.Debounced += _debouncer_Debounced;
        }

        private void _debouncer_Debounced(object sender, DebouncedEventArgs e)
        {
            try
            {
                Log.Info($"{_twitchUserLogin} 發送頻道更新通知 (Debouncer 觸發數量: {e.Count})");

                var description = string.Join("\n\n", messageQueue);

                // 匯流排開啟時 publish DTO、否則本地重建 embed 發送，統一由 TwitchService 處理
                Task.Run(async () => { await _twitchService.SendOrPublishChannelUpdateAsync(_twitchUserId, _twitchUserName, _twitchUserLogin, description); });
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), $"{_twitchUserLogin} 訊息去抖動失敗");
            }
            finally
            {
                messageQueue.Clear();
                _debouncer.Reset();
            }
        }

        public void AddMessage(string message)
        {
            Log.Debug($"DebounceChannelUpdateMessage ({_twitchUserLogin}): {message}");

            messageQueue.Enqueue(message);
            _debouncer.Trigger();
        }

        bool isDisposed;
        public void Dispose()
        {
            if (!isDisposed)
            {
                _debouncer.Debounced -= _debouncer_Debounced;
                _debouncer.Dispose();
                isDisposed = true;
            }
        }
    }
}
