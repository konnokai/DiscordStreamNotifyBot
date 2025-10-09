namespace DiscordStreamNotifyBot.SharedService.Youtube.Json
{
    public class NijisanjiLiverJson
    {
        [JsonProperty("slug")]
        public string Slug { get; set; }

        [JsonProperty("hidden")]
        public bool Hidden { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("enName")]
        public string EnName { get; set; }

        [JsonProperty("images")]
        public Images Images { get; set; }

        [Obsolete("官方目前已不再提供此資訊")]
        [JsonProperty("socialLinks")]
        public SocialLinks SocialLinks { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("subscriberCount")]
        public int SubscriberCount { get; set; }
    }

    public class Head
    {
        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("height")]
        public int Height { get; set; }

        [JsonProperty("width")]
        public int Width { get; set; }
    }

    public class Images
    {
        [JsonProperty("head")]
        public Head Head { get; set; }
    }

    public class SocialLinks
    {
        [JsonProperty("fieldId")]
        public string FieldId { get; set; }

        [JsonProperty("twitter")]
        public string Twitter { get; set; }

        [JsonProperty("youtube")]
        public string Youtube { get; set; }

        [JsonProperty("twitch")]
        public string Twitch { get; set; }

        [JsonProperty("reddit")]
        public string Reddit { get; set; }
    }
}
