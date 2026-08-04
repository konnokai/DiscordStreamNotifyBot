namespace DiscordStreamNotifyBot.Auth
{
    /// <summary>與 Backend 共用的 Twitch OAuth token 密文 payload 契約。</summary>
    public sealed class TwitchAccessTokenData
    {
        [JsonProperty(PropertyName = "access_token")]
        public string AccessToken { get; set; }

        [JsonProperty(PropertyName = "refresh_token")]
        public string RefreshToken { get; set; }

        [JsonProperty(PropertyName = "expires_in")]
        public int ExpiresIn { get; set; }

        [JsonProperty(PropertyName = "user_id")]
        public string TwitchUserId { get; set; }

        [JsonProperty(PropertyName = "scope")]
        public string[] Scopes { get; set; }

        [JsonProperty(PropertyName = "token_type")]
        public string TokenType { get; set; }
    }

    public sealed class TwitchValidateTokenData
    {
        [JsonProperty("client_id")]
        public string ClientId { get; set; }

        [JsonProperty("login")]
        public string Login { get; set; }

        [JsonProperty("scopes")]
        public string[] Scopes { get; set; }

        [JsonProperty("user_id")]
        public string UserId { get; set; }

        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }
    }

    public sealed class TwitchTokenErrorData
    {
        [JsonProperty("error")]
        public string Error { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }
    }
}
