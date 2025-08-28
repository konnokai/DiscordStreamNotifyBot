using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace StreamNotifyBot.Crawler.WebhookHandlers;

/// <summary>
/// Twitch EventSub Webhook 處理器
/// 處理來自 Twitch 的 EventSub 事件
/// </summary>
public class TwitchWebhookHandler
{
    private readonly ILogger<TwitchWebhookHandler> _logger;
    private readonly IDatabase _redis;
    private readonly string _webhookSecret;

    public TwitchWebhookHandler(
        ILogger<TwitchWebhookHandler> logger,
        IConnectionMultiplexer redis)
    {
        _logger = logger;
        _redis = redis.GetDatabase();
        
        // 從 Redis 取得 Webhook Secret
        var secret = _redis.StringGet("twitch:webhook_secret");
        _webhookSecret = secret.HasValue ? secret.ToString() : string.Empty;
        if (string.IsNullOrEmpty(_webhookSecret))
        {
            throw new InvalidOperationException("Twitch webhook secret not found in Redis");
        }
    }

    /// <summary>
    /// 處理 Twitch EventSub Webhook 請求
    /// </summary>
    /// <param name="context">HTTP 內容</param>
    public async Task HandleWebhookAsync(HttpContext context)
    {
        try
        {
            // 讀取請求內容
            using var reader = new StreamReader(context.Request.Body);
            var body = await reader.ReadToEndAsync();

            // 驗證簽名
            if (!VerifySignature(context.Request.Headers, body))
            {
                _logger.LogWarning("Invalid Twitch webhook signature");
                context.Response.StatusCode = 401;
                return;
            }

            // 取得訊息類型
            var messageType = context.Request.Headers["Twitch-Eventsub-Message-Type"].FirstOrDefault();
            
            switch (messageType)
            {
                case "webhook_callback_verification":
                    await HandleVerificationAsync(context, body);
                    break;
                
                case "notification":
                    await HandleNotificationAsync(context, body);
                    break;
                
                case "revocation":
                    await HandleRevocationAsync(context, body);
                    break;
                
                default:
                    _logger.LogWarning("Unknown Twitch EventSub message type: {MessageType}", messageType);
                    context.Response.StatusCode = 400;
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling Twitch webhook");
            context.Response.StatusCode = 500;
        }
    }

    /// <summary>
    /// 處理驗證回調
    /// </summary>
    private async Task HandleVerificationAsync(HttpContext context, string body)
    {
        try
        {
            var verification = JsonSerializer.Deserialize<TwitchVerificationRequest>(body);
            if (verification?.Challenge != null)
            {
                _logger.LogInformation("Twitch webhook verification successful");
                context.Response.ContentType = "text/plain";
                await context.Response.WriteAsync(verification.Challenge);
                return;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling Twitch webhook verification");
        }

        context.Response.StatusCode = 400;
    }

    /// <summary>
    /// 處理通知事件
    /// </summary>
    private async Task HandleNotificationAsync(HttpContext context, string body)
    {
        try
        {
            var notification = JsonSerializer.Deserialize<TwitchEventSubNotification>(body);
            if (notification?.Event == null)
            {
                context.Response.StatusCode = 400;
                return;
            }

            var subscriptionType = notification.Subscription?.Type;
            _logger.LogDebug("Received Twitch EventSub notification: {Type}", subscriptionType);

            switch (subscriptionType)
            {
                case "stream.offline":
                    await HandleStreamOfflineAsync(notification.Event);
                    break;
                
                case "channel.update":
                    await HandleChannelUpdateAsync(notification.Event);
                    break;
                
                default:
                    _logger.LogWarning("Unhandled Twitch EventSub type: {Type}", subscriptionType);
                    break;
            }

            context.Response.StatusCode = 204; // No Content
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling Twitch notification");
            context.Response.StatusCode = 500;
        }
    }

    /// <summary>
    /// 處理撤銷事件
    /// </summary>
    private async Task HandleRevocationAsync(HttpContext context, string body)
    {
        try
        {
            var revocation = JsonSerializer.Deserialize<TwitchEventSubNotification>(body);
            var subscriptionId = revocation?.Subscription?.Id;
            
            _logger.LogWarning("Twitch EventSub subscription revoked: {SubscriptionId}", subscriptionId);
            
            // TODO: 處理訂閱撤銷邏輯，可能需要重新訂閱
            
            context.Response.StatusCode = 204;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling Twitch revocation");
            context.Response.StatusCode = 500;
        }
        
        await Task.CompletedTask;
    }

    /// <summary>
    /// 處理直播離線事件
    /// </summary>
    private async Task HandleStreamOfflineAsync(JsonElement eventData)
    {
        try
        {
            var streamOffline = JsonSerializer.Deserialize<TwitchStreamOfflineEvent>(eventData.GetRawText());
            if (streamOffline == null) return;

            _logger.LogInformation("Twitch stream offline: {UserLogin} ({UserId})", 
                streamOffline.BroadcasterUserLogin, streamOffline.BroadcasterUserId);

            // 發送到 Redis 供 TwitchMonitor 處理
            await _redis.PublishAsync(
                RedisChannel.Literal("twitch:stream_offline"),
                JsonSerializer.Serialize(streamOffline));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling Twitch stream offline event");
        }
    }

    /// <summary>
    /// 處理頻道更新事件
    /// </summary>
    private async Task HandleChannelUpdateAsync(JsonElement eventData)
    {
        try
        {
            var channelUpdate = JsonSerializer.Deserialize<TwitchChannelUpdateEvent>(eventData.GetRawText());
            if (channelUpdate == null) return;

            _logger.LogDebug("Twitch channel update: {UserLogin} - {Title}", 
                channelUpdate.BroadcasterUserLogin, channelUpdate.Title);

            // 發送到 Redis 供 TwitchMonitor 處理
            await _redis.PublishAsync(
                RedisChannel.Literal("twitch:channel_update"),
                JsonSerializer.Serialize(channelUpdate));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling Twitch channel update event");
        }
    }

    /// <summary>
    /// 驗證 Twitch Webhook 簽名
    /// </summary>
    private bool VerifySignature(IHeaderDictionary headers, string body)
    {
        try
        {
            var signature = headers["Twitch-Eventsub-Message-Signature"].FirstOrDefault();
            var timestamp = headers["Twitch-Eventsub-Message-Timestamp"].FirstOrDefault();
            var messageId = headers["Twitch-Eventsub-Message-Id"].FirstOrDefault();

            if (string.IsNullOrEmpty(signature) || string.IsNullOrEmpty(timestamp) || string.IsNullOrEmpty(messageId))
            {
                return false;
            }

            // 建構要驗證的訊息
            var message = messageId + timestamp + body;
            
            // 計算 HMAC-SHA256
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_webhookSecret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
            var expectedSignature = "sha256=" + Convert.ToHexString(hash).ToLower();

            return string.Equals(signature, expectedSignature, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying Twitch webhook signature");
            return false;
        }
    }
}

#region Event Models

/// <summary>
/// Twitch 驗證請求模型
/// </summary>
public class TwitchVerificationRequest
{
    public string Challenge { get; set; } = "";
    public TwitchSubscription? Subscription { get; set; }
}

/// <summary>
/// Twitch EventSub 通知模型
/// </summary>
public class TwitchEventSubNotification
{
    public TwitchSubscription? Subscription { get; set; }
    public JsonElement Event { get; set; }
}

/// <summary>
/// Twitch 訂閱資訊模型
/// </summary>
public class TwitchSubscription
{
    public string Id { get; set; } = "";
    public string Type { get; set; } = "";
    public string Version { get; set; } = "";
    public string Status { get; set; } = "";
    public int Cost { get; set; }
    public Dictionary<string, string> Condition { get; set; } = new();
    public TwitchTransport? Transport { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Twitch 傳輸資訊模型
/// </summary>
public class TwitchTransport
{
    public string Method { get; set; } = "";
    public string Callback { get; set; } = "";
}

/// <summary>
/// Twitch 直播離線事件模型
/// </summary>
public class TwitchStreamOfflineEvent
{
    [JsonPropertyName("broadcaster_user_id")]
    public string BroadcasterUserId { get; set; } = "";
    
    [JsonPropertyName("broadcaster_user_login")]
    public string BroadcasterUserLogin { get; set; } = "";
    
    [JsonPropertyName("broadcaster_user_name")]
    public string BroadcasterUserName { get; set; } = "";
}

/// <summary>
/// Twitch 頻道更新事件模型
/// </summary>
public class TwitchChannelUpdateEvent
{
    [JsonPropertyName("broadcaster_user_id")]
    public string BroadcasterUserId { get; set; } = "";
    
    [JsonPropertyName("broadcaster_user_login")]
    public string BroadcasterUserLogin { get; set; } = "";
    
    [JsonPropertyName("broadcaster_user_name")]
    public string BroadcasterUserName { get; set; } = "";
    
    [JsonPropertyName("title")]
    public string Title { get; set; } = "";
    
    [JsonPropertyName("language")]
    public string Language { get; set; } = "";
    
    [JsonPropertyName("category_id")]
    public string CategoryId { get; set; } = "";
    
    [JsonPropertyName("category_name")]
    public string CategoryName { get; set; } = "";
    
    [JsonPropertyName("is_mature")]
    public bool IsMature { get; set; }
}

#endregion
