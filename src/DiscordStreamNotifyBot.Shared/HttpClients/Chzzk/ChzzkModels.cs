using Newtonsoft.Json;

namespace DiscordStreamNotifyBot.HttpClients.Chzzk.Model
{
    /// <summary>CHZZK 回應外層：<c>code</c> 為業務狀態碼，資料在 <c>content</c>。</summary>
    internal sealed class ChzzkEnvelope<T>
    {
        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("content")]
        public T Content { get; set; }
    }

    /// <summary>網站頻道資料 <c>service/v1/channels/{channelId}</c> 的必要欄位。</summary>
    public sealed class ChzzkChannel
    {
        [JsonProperty("channelId")]
        public string ChannelId { get; set; }

        [JsonProperty("channelName")]
        public string ChannelName { get; set; }

        [JsonProperty("channelImageUrl")]
        public string ChannelImageUrl { get; set; }

        [JsonProperty("openLive")]
        public bool OpenLive { get; set; }
    }

    /// <summary>
    /// 網站直播狀態 <c>polling/v3.1/channels/{channelId}/live-status</c> 的必要欄位。
    /// <para>
    /// 生命週期一律使用頂層 <c>status</c>；<c>livePollingStatusJson</c> 為播放器診斷欄位，CLOSE 樣本仍可能是
    /// STARTED，不得作為判定來源。
    /// </para>
    /// </summary>
    public sealed class ChzzkLiveStatus
    {
        [JsonProperty("channelId")]
        public string ChannelId { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("openDate")]
        public string OpenDate { get; set; }

        [JsonProperty("closeDate")]
        public string CloseDate { get; set; }

        [JsonProperty("liveTitle")]
        public string LiveTitle { get; set; }

        [JsonProperty("liveCategoryValue")]
        public string LiveCategoryValue { get; set; }
    }
}
