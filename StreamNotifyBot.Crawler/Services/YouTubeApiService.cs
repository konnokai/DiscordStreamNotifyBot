using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StreamNotifyBot.Crawler.Configuration;
using System.Net;
using System.Text.RegularExpressions;
using DiscordStreamNotifyBot.DataBase;

namespace StreamNotifyBot.Crawler.Services;

/// <summary>
/// YouTube API 核心服務
/// 遷移自 SharedService/Youtube/YoutubeStreamService.cs 的核心 API 方法
/// 整合了錯誤處理和重試機制
/// </summary>
public class YouTubeApiService
{
    private readonly ILogger<YouTubeApiService> _logger;
    private readonly YouTubeConfig _config;
    private readonly MainDbContext _dbContext;
    private readonly YouTubeQuotaManager _quotaManager;
    private readonly YouTubeApiErrorHandler _errorHandler;

    public YouTubeApiService(
        ILogger<YouTubeApiService> logger,
        IOptions<CrawlerConfig> config,
        MainDbContext dbContext,
        YouTubeQuotaManager quotaManager,
        YouTubeApiErrorHandler errorHandler)
    {
        _logger = logger;
        _config = config.Value.Platforms.YouTube;
        _dbContext = dbContext;
        _quotaManager = quotaManager;
        _errorHandler = errorHandler;
    }

    /// <summary>
    /// 取得影片詳細資訊
    /// 遷移自 YoutubeStreamService.GetVideoAsync，使用新的錯誤處理器
    /// </summary>
    public async Task<Video?> GetVideoAsync(string videoId)
    {
        return await _errorHandler.ExecuteWithRetryAsync(async () =>
        {
            var service = await _quotaManager.GetAvailableServiceAsync();
            var request = service.Videos.List("snippet,statistics,liveStreamingDetails");
            request.Id = videoId;

            var response = await request.ExecuteAsync();
            
            // 記錄配額使用
            _quotaManager.RecordQuotaUsage("videos.list", 1);
            
            var video = response.Items?.FirstOrDefault();
            if (video != null)
            {
                _logger.LogDebug("Successfully retrieved video data for {VideoId}: {Title}", 
                    videoId, video.Snippet?.Title);
            }
            else
            {
                _logger.LogWarning("Video not found or inaccessible: {VideoId}", videoId);
            }

            return video;
        }, $"GetVideo_{videoId}");
    }

    /// <summary>
    /// 從 YouTube URL 或頻道名稱解析頻道 ID
    /// 遷移自 YoutubeStreamService.GetChannelIdAsync
    /// </summary>
    public async Task<string?> GetChannelIdAsync(string channelUrl)
    {
        return await _errorHandler.ExecuteWithRetryAsync(async () =>
        {
            // 如果已經是頻道 ID 格式，直接返回
            if (channelUrl.StartsWith("UC") && channelUrl.Length == 24)
            {
                return channelUrl;
            }

            // 從 URL 中提取頻道 ID 或用戶名
            var channelId = ExtractChannelIdFromUrl(channelUrl);
            if (!string.IsNullOrEmpty(channelId) && channelId.StartsWith("UC"))
            {
                return channelId;
            }

            // 如果是頻道名稱或自定義 URL，需要通過 API 解析
            var service = await _quotaManager.GetAvailableServiceAsync();
            
            // 先嘗試通過搜索 API 查找
            var searchRequest = service.Search.List("snippet");
            searchRequest.Q = channelUrl;
            searchRequest.Type = "channel";
            searchRequest.MaxResults = 1;

            var searchResponse = await searchRequest.ExecuteAsync();
            _quotaManager.RecordQuotaUsage("search.list", 100);

            var channel = searchResponse.Items?.FirstOrDefault();
            if (channel?.Snippet?.ChannelId != null)
            {
                _logger.LogDebug("Found channel ID {ChannelId} for search term: {SearchTerm}", 
                    channel.Snippet.ChannelId, channelUrl);
                return channel.Snippet.ChannelId;
            }

            _logger.LogWarning("Could not resolve channel ID for: {ChannelUrl}", channelUrl);
            return null;
        }, $"GetChannelId_{channelUrl}");
    }

    /// <summary>
    /// 取得單一頻道標題
    /// 遷移自 YoutubeStreamService.GetChannelTitle
    /// </summary>
    public async Task<string?> GetChannelTitle(string channelId)
    {
        return await _errorHandler.ExecuteWithRetryAsync(async () =>
        {
            var service = await _quotaManager.GetAvailableServiceAsync();
            var request = service.Channels.List("snippet");
            request.Id = channelId;

            var response = await request.ExecuteAsync();
            _quotaManager.RecordQuotaUsage("channels.list", 1);

            var channel = response.Items?.FirstOrDefault();
            if (channel?.Snippet?.Title != null)
            {
                _logger.LogDebug("Retrieved channel title for {ChannelId}: {Title}", 
                    channelId, channel.Snippet.Title);
                return channel.Snippet.Title;
            }

            _logger.LogWarning("Channel title not found for: {ChannelId}", channelId);
            return null;
        }, $"GetChannelTitle_{channelId}");
    }

    /// <summary>
    /// 批量取得頻道標題
    /// 遷移自 YoutubeStreamService.GetChannelTitle(IEnumerable)
    /// </summary>
    public async Task<List<string>> GetChannelTitle(IEnumerable<string> channelIds)
    {
        var channelIdList = channelIds.ToList();
        if (!channelIdList.Any())
        {
            return new List<string>();
        }

        return await _errorHandler.ExecuteWithRetryAsync(async () =>
        {
            var results = new List<string>();
            const int batchSize = 50; // YouTube API 每次最多查詢 50 個頻道

            for (int i = 0; i < channelIdList.Count; i += batchSize)
            {
                var batch = channelIdList.Skip(i).Take(batchSize).ToList();
                
                var service = await _quotaManager.GetAvailableServiceAsync();
                var request = service.Channels.List("snippet");
                request.Id = string.Join(",", batch);

                var response = await request.ExecuteAsync();
                _quotaManager.RecordQuotaUsage("channels.list", 1);

                foreach (var channel in response.Items ?? Enumerable.Empty<Channel>())
                {
                    if (channel.Snippet?.Title != null)
                    {
                        results.Add(channel.Snippet.Title);
                    }
                }

                // 批次之間稍作延遲
                if (i + batchSize < channelIdList.Count)
                {
                    await Task.Delay(100);
                }
            }

            _logger.LogDebug("Retrieved {Count} channel titles from {RequestedCount} channels", 
                results.Count, channelIdList.Count);
            return results;
        }, $"GetChannelTitles_Batch_{channelIdList.Count}");
    }

    /// <summary>
    /// 取得頻道的最新影片
    /// </summary>
    public async Task<IEnumerable<SearchResult>> GetChannelLatestVideosAsync(string channelId, int maxResults = 10)
    {
        return await _errorHandler.ExecuteWithRetryAsync(async () =>
        {
            var service = await _quotaManager.GetAvailableServiceAsync();
            var request = service.Search.List("snippet");
            request.ChannelId = channelId;
            request.Type = "video";
            request.Order = SearchResource.ListRequest.OrderEnum.Date;
            request.MaxResults = maxResults;

            var response = await request.ExecuteAsync();
            _quotaManager.RecordQuotaUsage("search.list", 100);

            _logger.LogDebug("Retrieved {Count} latest videos for channel {ChannelId}", 
                response.Items?.Count ?? 0, channelId);

            return response.Items ?? Enumerable.Empty<SearchResult>();
        }, $"GetLatestVideos_{channelId}");
    }

    /// <summary>
    /// 檢查影片是否為直播
    /// </summary>
    public async Task<bool> IsLiveVideoAsync(string videoId)
    {
        return await _errorHandler.ExecuteWithRetryAsync(async () =>
        {
            var video = await GetVideoAsync(videoId);
            
            if (video?.LiveStreamingDetails != null)
            {
                var isLive = video.LiveStreamingDetails.ActualStartTimeDateTimeOffset.HasValue && 
                           !video.LiveStreamingDetails.ActualEndTimeDateTimeOffset.HasValue;
                
                _logger.LogDebug("Video {VideoId} live status: {IsLive}", videoId, isLive);
                return isLive;
            }

            return false;
        }, $"IsLiveVideo_{videoId}");
    }

    #region Private Helper Methods

    /// <summary>
    /// 從 YouTube URL 中提取頻道 ID 或用戶名
    /// </summary>
    private string ExtractChannelIdFromUrl(string url)
    {
        try
        {
            // 標準頻道 URL 格式
            var patterns = new[]
            {
                @"youtube\.com/channel/([a-zA-Z0-9_-]+)",
                @"youtube\.com/c/([a-zA-Z0-9_-]+)",
                @"youtube\.com/user/([a-zA-Z0-9_-]+)",
                @"youtube\.com/@([a-zA-Z0-9_.-]+)"
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(url, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
            }

            // 如果沒有匹配到 URL 格式，可能是直接的頻道名稱
            return url;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract channel ID from URL: {Url}", url);
            return url;
        }
    }

    #endregion
}
