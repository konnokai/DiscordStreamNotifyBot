using System.Text;

namespace DiscordStreamNotifyBot.HttpClients
{
    public class DiscordWebhookClient
    {
        private readonly HttpClient _httpClient;
        private readonly DiscordSocketClient _client;
        private readonly BotConfig _botConfig;

        public DiscordWebhookClient(HttpClient httpClient, DiscordSocketClient client, BotConfig botConfig)
        {
            httpClient.DefaultRequestHeaders.Add("UserAgent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/96.0.4664.45 Safari/537.36");
            _httpClient = httpClient;
            _client = client;
            _botConfig = botConfig;
        }

        public void SendMessageToDiscord(string content)
        {
            string username;
            string avatarUrl;
            if (_client.CurrentUser != null)
            {
                username = _client.CurrentUser.Username;
                avatarUrl = _client.CurrentUser.GetAvatarUrl();
            }
            else
            {
                username = "Bot";
                avatarUrl = "";
            }

            _ = SendMessageToDiscordAsync(_httpClient, _botConfig.WebHookUrl, content, username, avatarUrl);
        }

        /// <summary>等待 Discord webhook 接受訊息，供程序即將結束、不能使用 fire-and-forget 的情境。</summary>
        public static async Task SendMessageToDiscordAsync(string webhookUrl, string content, string username, CancellationToken cancellationToken = default)
        {
            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            httpClient.DefaultRequestHeaders.Add("UserAgent", "DiscordStreamNotifyBot-CrashReporter");
            await SendMessageToDiscordAsync(httpClient, webhookUrl, content, username, "", cancellationToken);
        }

        private static async Task SendMessageToDiscordAsync(HttpClient httpClient, string webhookUrl, string content,
            string username, string avatarUrl, CancellationToken cancellationToken = default)
        {
            var message = new Message
            {
                username = username,
                avatar_url = avatarUrl,
                content = content,
            };
            using var httpContent = new StringContent(JsonConvert.SerializeObject(message), Encoding.UTF8, "application/json");
            using var response = await httpClient.PostAsync(webhookUrl, httpContent, cancellationToken);
            response.EnsureSuccessStatusCode();
        }

        class Message
        {
            public string username { get; set; }
            public string content { get; set; }
            public string avatar_url { get; set; }
        }
    }
}
