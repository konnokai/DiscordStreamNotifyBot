namespace DiscordStreamNotifyBot.SharedService.Youtube.Json
{
    public class Channel
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("thumbnail-url")]
        public string ThumbnailUrl { get; set; }

        [JsonProperty("main")]
        public bool? Main { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("liver")]
        public Liver Liver { get; set; }
    }

    public class EventLiver
    {
        [JsonProperty("external-id")]
        public string ExternalId { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }
    }

    public class Liver
    {
        [JsonProperty("external-id")]
        public string ExternalId { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }
    }

    public class NijisanjiStreamJson
    {
        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("thumbnail-url")]
        public string ThumbnailUrl { get; set; }

        [JsonProperty("fallback-thumbnail-url")]
        public string FallbackThumbnailUrl { get; set; }

        [JsonProperty("start-at")]
        public DateTime? StartAt { get; set; }

        [JsonProperty("end-at")]
        public DateTime? EndAt { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("platform")]
        public string Platform { get; set; }

        [JsonProperty("channel")]
        public Channel Channel { get; set; }

        [JsonProperty("event-livers")]
        public List<EventLiver> EventLivers { get; set; }
    }
}