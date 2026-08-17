namespace DiscordStreamNotifyBot.Shared
{
    internal static class YoutubeVideoIdParser
    {
        private static readonly string[] VideoPathPrefixes = { "live", "shorts", "embed", "v" };

        internal static string Parse(string videoUrlOrId)
        {
            if (string.IsNullOrEmpty(videoUrlOrId))
                throw new ArgumentNullException(videoUrlOrId);

            if (TryParse(videoUrlOrId, out string videoId))
                return videoId;

            throw new UriFormatException("影片網址格式錯誤，請確認輸入的是 YouTube 影片網址");
        }

        internal static bool TryParse(string videoUrlOrId, out string videoId)
        {
            videoId = null;
            if (string.IsNullOrWhiteSpace(videoUrlOrId))
                return false;

            string input = videoUrlOrId.Trim();
            if (IsVideoId(input))
            {
                videoId = input;
                return true;
            }

            if (!TryCreateHttpUri(input, out Uri uri))
                return false;

            try
            {
                if (IsHost(uri.Host, "youtu.be"))
                    return TryUseId(uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault(), out videoId);

                if (!IsHost(uri.Host, "youtube.com") && !IsHost(uri.Host, "youtube-nocookie.com"))
                    return false;

                string[] path = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (path.Length == 1 && string.Equals(path[0], "watch", StringComparison.OrdinalIgnoreCase))
                    return TryGetQueryValue(uri.Query, "v", out videoId);

                if (path.Length >= 2 && VideoPathPrefixes.Contains(path[0], StringComparer.OrdinalIgnoreCase))
                    return TryUseId(path[1], out videoId);

                return false;
            }
            catch (UriFormatException)
            {
                return false;
            }
        }

        private static bool TryCreateHttpUri(string input, out Uri uri)
        {
            string candidate = input.StartsWith("//", StringComparison.Ordinal)
                ? "https:" + input
                : input;

            if (!Uri.TryCreate(candidate, UriKind.Absolute, out uri) || string.IsNullOrEmpty(uri.Host))
            {
                if (!Uri.TryCreate("https://" + input.TrimStart('/'), UriKind.Absolute, out uri))
                    return false;
            }

            return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
        }

        private static bool TryGetQueryValue(string query, string expectedName, out string videoId)
        {
            videoId = null;
            foreach (string pair in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                int separator = pair.IndexOf('=');
                string name = Uri.UnescapeDataString(separator < 0 ? pair : pair[..separator]);
                if (!string.Equals(name, expectedName, StringComparison.Ordinal))
                    continue;

                string value = separator < 0 ? "" : pair[(separator + 1)..];
                if (TryUseId(value, out videoId))
                    return true;
            }

            return false;
        }

        private static bool TryUseId(string value, out string videoId)
        {
            videoId = null;
            if (string.IsNullOrEmpty(value))
                return false;

            string decoded = Uri.UnescapeDataString(value);
            if (!IsVideoId(decoded))
                return false;

            videoId = decoded;
            return true;
        }

        private static bool IsHost(string host, string expectedHost)
            => string.Equals(host, expectedHost, StringComparison.OrdinalIgnoreCase)
                || host.EndsWith("." + expectedHost, StringComparison.OrdinalIgnoreCase);

        private static bool IsVideoId(string value)
            => value.Length == 11 && value.All(character =>
                character is >= 'a' and <= 'z'
                or >= 'A' and <= 'Z'
                or >= '0' and <= '9'
                or '-' or '_');
    }
}
