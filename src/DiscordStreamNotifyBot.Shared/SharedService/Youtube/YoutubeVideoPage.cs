using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DiscordStreamNotifyBot.SharedService.Youtube
{
    public enum YoutubeVideoPageStatus { Unknown, Public, MembersOnly, Private, Unavailable }

    /// <summary>
    /// 以未登入的觀看頁 playabilityStatus 判斷影片狀態。
    /// 留言 API 的錯誤碼分不清私人、已刪除與會限影片，觀看頁可以直接看出來。
    /// 錄影工具的 YouTubeVideoPage 是同一套規則，改判斷時兩邊要一起改。
    /// </summary>
    public static class YoutubeVideoPage
    {
        private const string PlayerResponseMarker = "var ytInitialPlayerResponse = ";
        private static readonly HttpClient HttpClient = CreateHttpClient();

        /// <summary>讀不到頁面或格式不符時回傳 Unknown，呼叫端應退回原本的 API 判斷。</summary>
        public static async Task<YoutubeVideoPageStatus> GetStatusAsync(string videoId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(videoId) || videoId == "-")
                return YoutubeVideoPageStatus.Unknown;

            try
            {
                // hl=en 讓私人影片的 reason 固定是英文
                string html = await HttpClient.GetStringAsync(
                    $"https://www.youtube.com/watch?v={Uri.EscapeDataString(videoId)}&hl=en", cancellationToken).ConfigureAwait(false);
                YoutubeVideoPageStatus status = Parse(html);
                if (status == YoutubeVideoPageStatus.Unknown)
                    Log.Warn($"YouTube 觀看頁無法判斷影片狀態: {videoId}");
                return status;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                Log.Warn($"讀取 YouTube 觀看頁失敗: {videoId} {ex.Message}");
                return YoutubeVideoPageStatus.Unknown;
            }
        }

        public static YoutubeVideoPageStatus Parse(string html)
        {
            int start = html?.IndexOf(PlayerResponseMarker, StringComparison.Ordinal) ?? -1;
            if (start < 0)
                return YoutubeVideoPageStatus.Unknown;

            JToken playability;
            try
            {
                // JObject.Load 讀完第一個物件就停，不用自己找 JSON 結尾
                using var reader = new JsonTextReader(new StringReader(html[(start + PlayerResponseMarker.Length)..]));
                playability = JObject.Load(reader)["playabilityStatus"];
            }
            catch (JsonException)
            {
                return YoutubeVideoPageStatus.Unknown;
            }

            string status = playability?.Value<string>("status");
            string reason = playability?.Value<string>("reason") ?? string.Empty;
            if (playability?.SelectToken("errorScreen.playerLegacyDesktopYpcOfferRenderer.offerId")?.ToString() == "sponsors_only_video")
                return YoutubeVideoPageStatus.MembersOnly;
            // 年齡限制與「確認你不是機器人」也是 LOGIN_REQUIRED，要再看 reason
            if (status == "LOGIN_REQUIRED" && reason.Contains("private", StringComparison.OrdinalIgnoreCase))
                return YoutubeVideoPageStatus.Private;

            return status switch
            {
                "OK" => YoutubeVideoPageStatus.Public,
                "ERROR" => YoutubeVideoPageStatus.Unavailable,
                _ => YoutubeVideoPageStatus.Unknown
            };
        }

        private static HttpClient CreateHttpClient()
        {
            var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            client.DefaultRequestHeaders.TryAddWithoutValidation(
                "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36");
            client.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Language", "en-US,en;q=0.9");
            return client;
        }
    }
}
