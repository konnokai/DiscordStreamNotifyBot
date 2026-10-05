using DiscordStreamNotifyBot.Interaction;
using DiscordStreamNotifyBot.Localization;

namespace DiscordStreamNotifyBot.SharedService
{
    public class EmojiService : IInteractionService
    {
        public Emote YouTubeEmote { get; private set; }
        public Emote PayPalEmote { get; private set; }
        public Emote ECPayEmote { get; private set; }

        private readonly DiscordSocketClient _client;

        public EmojiService(DiscordSocketClient client, BotConfig botConfig)
        {
            _client = client;

#if !RELEASE
            return;
#endif

            try
            {
                YouTubeEmote = _client.GetApplicationEmoteAsync(botConfig.YouTubeEmoteId).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Log.Error($"無法取得 YouTube Emote: {ex}");
                YouTubeEmote = null;
            }

            try
            {
                PayPalEmote = _client.GetApplicationEmoteAsync(botConfig.PayPalEmoteId).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Log.Error($"無法取得 PayPal Emote: {ex}");
                PayPalEmote = null;
            }

            try
            {
                ECPayEmote = _client.GetApplicationEmoteAsync(botConfig.ECPayEmoteId).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Log.Error($"無法取得 ECPay Emote: {ex}");
                ECPayEmote = null;
            }
        }

        /// <summary>開台通知附帶的隨機影片與贊助按鈕；是否停用廣告由呼叫端依 DisableNotificationsAds 判斷。</summary>
        public MessageComponent BuildNotificationAdsComponent(BotLocalizer localizer, string locale)
            => new ComponentBuilder()
                .WithButton(localizer.Get("Notifications.Button.RandomVideo", locale), style: ButtonStyle.Link,
                    emote: YouTubeEmote, url: "https://api.konnokai.me/randomvideo")
                .WithButton(localizer.Get("Notifications.Button.SupportEcpay", locale), style: ButtonStyle.Link,
                    emote: ECPayEmote, url: Utility.ECPayUrl, row: 1)
                .WithButton(localizer.Get("Notifications.Button.SupportPaypal", locale), style: ButtonStyle.Link,
                    emote: PayPalEmote, url: Utility.PaypalUrl, row: 1)
                .Build();
    }
}
