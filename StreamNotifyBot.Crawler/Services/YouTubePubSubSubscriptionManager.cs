using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StreamNotifyBot.Crawler.Configuration;
using DiscordStreamNotifyBot.DataBase;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace StreamNotifyBot.Crawler.Services;

public class YouTubePubSubSubscriptionManager
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<YouTubePubSubSubscriptionManager> _logger;
    private readonly CrawlerConfig _config;
    private readonly MainDbService _dbService;
    private bool _isSubscribing = false;

    public YouTubePubSubSubscriptionManager(
        IHttpClientFactory httpClientFactory,
        ILogger<YouTubePubSubSubscriptionManager> logger,
        IOptions<CrawlerConfig> config,
        MainDbService dbService)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _config = config.Value;
        _dbService = dbService;
    }

    /// <summary>
    /// 訂閱或取消訂閱 YouTube 頻道的 PubSubHubbub 通知
    /// </summary>
    /// <param name="channelId">YouTube 頻道 ID</param>
    /// <param name="subscribe">true = 訂閱, false = 取消訂閱</param>
    /// <returns>是否成功</returns>
    public async Task<bool> PostSubscribeRequestAsync(string channelId, bool subscribe = true)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            using var request = new HttpRequestMessage();

            request.RequestUri = new("https://pubsubhubbub.appspot.com/subscribe");
            request.Method = HttpMethod.Post;
            string guid = Guid.NewGuid().ToString();

            var formList = new Dictionary<string, string>()
            {
                { "hub.mode", subscribe ? "subscribe" : "unsubscribe" },
                { "hub.topic", $"https://www.youtube.com/xml/feeds/videos.xml?channel_id={channelId}" },
                { "hub.callback", $"https://{_config.Platforms.YouTube.WebhookCallbackUrl}/NotificationCallback" },
                { "hub.verify", "async" },
                { "hub.secret", guid },
                { "hub.verify_token", guid },
                { "hub.lease_seconds", "864000"}
            };

            request.Content = new FormUrlEncodedContent(formList);
            var response = await httpClient.SendAsync(request);
            var result = response.StatusCode == HttpStatusCode.Accepted;
            
            if (!result)
            {
                _logger.LogError("YouTube PubSub subscription failed for channel {ChannelId}: {StatusCode} - {Content}", 
                    channelId, response.StatusCode, await response.Content.ReadAsStringAsync());
                return false;
            }

            _logger.LogDebug("YouTube PubSub subscription {Action} successfully for channel {ChannelId}", 
                subscribe ? "registered" : "unregistered", channelId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "YouTube PubSub subscription failed for channel {ChannelId}", channelId);
            return false;
        }
    }

    /// <summary>
    /// 批量處理過期的 PubSubHubbub 訂閱（每 30 分鐘執行一次）
    /// </summary>
    public async Task ProcessExpiredSubscriptionsAsync()
    {
        if (_isSubscribing)
            return;
        
        _isSubscribing = true;

        try
        {
            using var db = _dbService.GetDbContext();
            var expiredChannels = await db.YoutubeChannelSpider
                .Where(x => x.LastSubscribeTime < DateTime.Now.AddDays(-7))
                .ToListAsync();

            int successCount = 0;
            int totalCount = expiredChannels.Count;

            if (totalCount == 0)
            {
                _logger.LogDebug("No expired YouTube PubSub subscriptions to renew");
                return;
            }

            _logger.LogInformation("Processing {Count} expired YouTube PubSub subscriptions", totalCount);

            foreach (var channel in expiredChannels)
            {
                try
                {
                    if (await PostSubscribeRequestAsync(channel.ChannelId))
                    {
                        successCount++;
                        _logger.LogInformation("Renewed YouTube PubSub subscription: {ChannelTitle} ({ChannelId}) ({Current}/{Total})",
                            channel.ChannelTitle, channel.ChannelId, successCount, totalCount);
                        
                        channel.LastSubscribeTime = DateTime.Now;
                        db.Update(channel);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to renew YouTube PubSub subscription: {ChannelTitle} ({ChannelId}) ({Current}/{Total})",
                            channel.ChannelTitle, channel.ChannelId, successCount + 1, totalCount);
                    }

                    // 避免過於頻繁的請求
                    await Task.Delay(100);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing PubSub subscription for channel {ChannelId}", channel.ChannelId);
                }
            }

            await db.SaveChangesAsync();
            _logger.LogInformation("Completed processing YouTube PubSub subscriptions: {Success}/{Total} successful",
                successCount, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing expired YouTube PubSub subscriptions");
        }
        finally
        {
            _isSubscribing = false;
        }
    }

    /// <summary>
    /// 為新加入的頻道訂閱 PubSubHubbub
    /// </summary>
    /// <param name="channelId">YouTube 頻道 ID</param>
    /// <returns>是否成功</returns>
    public async Task<bool> SubscribeNewChannelAsync(string channelId)
    {
        try
        {
            var success = await PostSubscribeRequestAsync(channelId, true);
            if (success)
            {
                // 更新資料庫記錄
                using var db = _dbService.GetDbContext();
                var spider = await db.YoutubeChannelSpider
                    .FirstOrDefaultAsync(x => x.ChannelId == channelId);
                    
                if (spider != null)
                {
                    spider.LastSubscribeTime = DateTime.Now;
                    db.Update(spider);
                    await db.SaveChangesAsync();
                }

                _logger.LogInformation("Successfully subscribed to new YouTube channel: {ChannelId}", channelId);
            }
            
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to subscribe new YouTube channel: {ChannelId}", channelId);
            return false;
        }
    }

    /// <summary>
    /// 取消訂閱已移除的頻道
    /// </summary>
    /// <param name="channelId">YouTube 頻道 ID</param>
    /// <returns>是否成功</returns>
    public async Task<bool> UnsubscribeChannelAsync(string channelId)
    {
        try
        {
            var success = await PostSubscribeRequestAsync(channelId, false);
            if (success)
            {
                _logger.LogInformation("Successfully unsubscribed from YouTube channel: {ChannelId}", channelId);
            }
            
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unsubscribe YouTube channel: {ChannelId}", channelId);
            return false;
        }
    }
}
