using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using StreamNotifyBot.Crawler.Configuration;
using StreamNotifyBot.Crawler.Models;
using DiscordStreamNotifyBot.DataBase;
using DiscordStreamNotifyBot.DataBase.Table;
using System.Text.Json;
using StackExchange.Redis;

namespace StreamNotifyBot.Crawler.Services;

/// <summary>
/// YouTube 直播狀態監控和變化檢測服務
/// </summary>
public class YouTubeStreamMonitorService
{
    private readonly ILogger<YouTubeStreamMonitorService> _logger;
    private readonly YouTubeConfig _config;
    private readonly YouTubeApiService _youtubeApiService;
    private readonly MainDbService _dbService;
    private readonly IConnectionMultiplexer _redis;
    private readonly ISubscriber _subscriber;
    private readonly YouTubeEventService _eventService;
    private readonly YouTubeTrackingManager _trackingManager;

    // 狀態快取，用於比較變化
    private readonly Dictionary<string, StreamStatus> _previousStreamStates = new();
    private readonly SemaphoreSlim _monitorSemaphore = new(1, 1);

    public YouTubeStreamMonitorService(
        ILogger<YouTubeStreamMonitorService> logger,
        IOptions<CrawlerConfig> config,
        YouTubeApiService youtubeApiService,
        MainDbService dbService,
        IConnectionMultiplexer redis,
        YouTubeEventService eventService,
        YouTubeTrackingManager trackingManager)
    {
        _logger = logger;
        _config = config.Value.Platforms.YouTube;
        _youtubeApiService = youtubeApiService;
        _dbService = dbService;
        _redis = redis;
        _subscriber = redis.GetSubscriber();
        _eventService = eventService;
        _trackingManager = trackingManager;
    }

    /// <summary>
    /// 監控 Holo 成員的直播狀態
    /// </summary>
    public async Task CheckHoloScheduleAsync()
    {
        if (!await _monitorSemaphore.WaitAsync(100))
        {
            _logger.LogDebug("Holo schedule check is already running, skipping");
            return;
        }

        try
        {
            _logger.LogDebug("Starting Holo schedule monitoring");
            
            using var db = _dbService.GetDbContext();
            
            // 獲取所有 Holo 相關頻道
            var holoChannels = await db.YoutubeChannelSpider
                .Where(x => x.ChannelId.Contains("UC"))
                .AsNoTracking()
                .ToListAsync();

            // 進一步篩選 Holo 頻道（通過標題判斷）並且只處理被追蹤的頻道
            holoChannels = holoChannels
                .Where(x => x.ChannelTitle != null && 
                           (x.ChannelTitle.Contains("hololive") || 
                            x.ChannelTitle.Contains("Holo")) &&
                           _trackingManager.IsChannelTracked(x.ChannelId))
                .ToList();

            if (holoChannels.Count == 0)
            {
                _logger.LogDebug("No tracked Holo channels found for monitoring");
                return;
            }

            _logger.LogDebug("Found {Count} tracked Holo channels to monitor", holoChannels.Count);
            await ProcessChannelBatchAsync(holoChannels, "Holo");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Holo schedule monitoring");
        }
        finally
        {
            _monitorSemaphore.Release();
        }
    }

    /// <summary>
    /// 監控彩虹社成員的直播狀態
    /// </summary>
    public async Task CheckNijisanjiScheduleAsync()
    {
        if (!await _monitorSemaphore.WaitAsync(100))
        {
            _logger.LogDebug("Nijisanji schedule check is already running, skipping");
            return;
        }

        try
        {
            _logger.LogDebug("Starting Nijisanji schedule monitoring");
            
            using var db = _dbService.GetDbContext();
            
            // 獲取所有彩虹社相關頻道
            var nijisanjiChannels = await db.YoutubeChannelSpider
                .Where(x => x.ChannelId.Contains("UC"))
                .AsNoTracking()
                .ToListAsync();

            // 進一步篩選彩虹社頻道（通過標題判斷）並且只處理被追蹤的頻道
            nijisanjiChannels = nijisanjiChannels
                .Where(x => x.ChannelTitle != null && 
                           (x.ChannelTitle.Contains("にじさんじ") || 
                            x.ChannelTitle.Contains("Nijisanji")) &&
                           _trackingManager.IsChannelTracked(x.ChannelId))
                .ToList();

            if (nijisanjiChannels.Count == 0)
            {
                _logger.LogDebug("No tracked Nijisanji channels found for monitoring");
                return;
            }

            _logger.LogDebug("Found {Count} tracked Nijisanji channels to monitor", nijisanjiChannels.Count);
            await ProcessChannelBatchAsync(nijisanjiChannels, "Nijisanji");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Nijisanji schedule monitoring");
        }
        finally
        {
            _monitorSemaphore.Release();
        }
    }

    /// <summary>
    /// 監控其他頻道的直播狀態
    /// </summary>
    public async Task CheckOtherChannelScheduleAsync()
    {
        if (!await _monitorSemaphore.WaitAsync(100))
        {
            _logger.LogDebug("Other channel schedule check is already running, skipping");
            return;
        }

        try
        {
            _logger.LogDebug("Starting other channels schedule monitoring");
            
            using var db = _dbService.GetDbContext();
            
            // 獲取其他頻道（不包含Holo、彩虹社的頻道）
            var otherChannels = await db.YoutubeChannelSpider
                .Where(x => x.ChannelId.Contains("UC"))
                .AsNoTracking()
                .ToListAsync();

            // 排除已知的Holo和彩虹社頻道並且只處理被追蹤的頻道
            otherChannels = otherChannels
                .Where(x => x.ChannelTitle != null && 
                           !x.ChannelTitle.Contains("Holo") && 
                           !x.ChannelTitle.Contains("holo") &&
                           !x.ChannelTitle.Contains("にじさんじ") && 
                           !x.ChannelTitle.Contains("Nijisanji") &&
                           _trackingManager.IsChannelTracked(x.ChannelId))
                .ToList();

            if (otherChannels.Count == 0)
            {
                _logger.LogDebug("No tracked other channels found for monitoring");
                return;
            }

            _logger.LogDebug("Found {Count} tracked other channels to monitor", otherChannels.Count);
            await ProcessChannelBatchAsync(otherChannels, "Other");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in other channels schedule monitoring");
        }
        finally
        {
            _monitorSemaphore.Release();
        }
    }

    /// <summary>
    /// 批量處理頻道監控
    /// </summary>
    private async Task ProcessChannelBatchAsync(List<YoutubeChannelSpider> channels, string group)
    {
        const int batchSize = 10; // 每批處理 10 個頻道以避免 API 配額快速耗盡
        var batches = channels.Select((channel, index) => new { channel, index })
                             .GroupBy(x => x.index / batchSize)
                             .Select(g => g.Select(x => x.channel).ToList())
                             .ToList();

        _logger.LogDebug("Processing {Group} channels in {BatchCount} batches", group, batches.Count);

        foreach (var batch in batches)
        {
            try
            {
                await ProcessSingleBatchAsync(batch, group);
                
                // 批次之間稍作延遲，避免過於頻繁的 API 調用
                if (batches.Count > 1)
                {
                    await Task.Delay(1000);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing {Group} batch with {Count} channels", group, batch.Count);
            }
        }
    }

    /// <summary>
    /// 處理單一批次的頻道
    /// </summary>
    private async Task ProcessSingleBatchAsync(List<YoutubeChannelSpider> channels, string group)
    {
        var channelIds = channels.Select(c => c.ChannelId).ToList();
        _logger.LogDebug("Processing batch for {Group}: {ChannelIds}", group, string.Join(", ", channelIds));

        foreach (var channel in channels)
        {
            try
            {
                await ProcessSingleChannelAsync(channel, group);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing channel {ChannelId} in {Group}", channel.ChannelId, group);
            }
        }
    }

    /// <summary>
    /// 處理單一頻道的監控
    /// </summary>
    private async Task ProcessSingleChannelAsync(YoutubeChannelSpider channel, string group)
    {
        try
        {
            _logger.LogDebug("Monitoring channel {ChannelTitle} ({ChannelId}) in {Group}", 
                channel.ChannelTitle, channel.ChannelId, group);

            // 使用 API 獲取頻道的當前直播狀態
            var videoData = await _youtubeApiService.GetVideoAsync(string.Empty); // 這裡需要正確的影片ID
            
            // 檢查頻道標題是否有變化
            var currentChannelTitle = await _youtubeApiService.GetChannelTitle(new List<string> { channel.ChannelId });
            if (currentChannelTitle?.FirstOrDefault() != null)
            {
                var newTitle = currentChannelTitle.First();
                if (!string.IsNullOrEmpty(newTitle) && newTitle != channel.ChannelTitle)
                {
                    _logger.LogInformation("Channel title changed for {ChannelId}: '{OldTitle}' → '{NewTitle}'", 
                        channel.ChannelId, channel.ChannelTitle, newTitle);
                    
                    // 廣播頻道更新事件
                    await _eventService.BroadcastChannelUpdateAsync(
                        channel.ChannelId, 
                        newTitle, 
                        new Dictionary<string, object>
                        {
                            ["Group"] = group,
                            ["PreviousTitle"] = channel.ChannelTitle ?? "Unknown",
                            ["UpdateType"] = "TitleChange"
                        });
                }
            }

            // 暫時記錄頻道監控完成
            _logger.LogDebug("Completed monitoring for channel {ChannelId} in {Group}", channel.ChannelId, group);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to monitor channel {ChannelId}", channel.ChannelId);
            
            // 廣播錯誤事件
            await _eventService.BroadcastErrorEventAsync(
                "ChannelMonitoringError",
                $"Failed to monitor channel {channel.ChannelId}: {ex.Message}",
                new Dictionary<string, object>
                {
                    ["ChannelId"] = channel.ChannelId,
                    ["Group"] = group,
                    ["ExceptionType"] = ex.GetType().Name
                });
        }
    }

    /// <summary>
    /// 檢查排程時間和狀態更新
    /// </summary>
    public async Task CheckScheduleTimeAsync()
    {
        try
        {
            _logger.LogDebug("Starting schedule time check");
            
            using var db = _dbService.GetDbContext();
            
            // 檢查即將開始的直播（未來 1 小時內）- 需要檢查所有影片表
            var holoStreams = await db.HoloVideos
                .Where(v => v.ScheduledStartTime > DateTime.Now &&
                           v.ScheduledStartTime <= DateTime.Now.AddHours(1))
                .AsNoTracking()
                .ToListAsync();

            var nijisanjiStreams = await db.NijisanjiVideos
                .Where(v => v.ScheduledStartTime > DateTime.Now &&
                           v.ScheduledStartTime <= DateTime.Now.AddHours(1))
                .AsNoTracking()
                .ToListAsync();

            var otherStreams = await db.OtherVideos
                .Where(v => v.ScheduledStartTime > DateTime.Now &&
                           v.ScheduledStartTime <= DateTime.Now.AddHours(1))
                .AsNoTracking()
                .ToListAsync();

            var allUpcomingStreams = holoStreams.Cast<Video>()
                .Concat(nijisanjiStreams.Cast<Video>())
                .Concat(otherStreams.Cast<Video>())
                .ToList();

            _logger.LogDebug("Found {Count} upcoming scheduled streams", allUpcomingStreams.Count);

            foreach (var stream in allUpcomingStreams)
            {
                try
                {
                    await CheckStreamScheduleStatusAsync(stream);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking schedule for stream {VideoId}", stream.VideoId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in schedule time check");
        }
    }

    /// <summary>
    /// 檢查單一直播的排程狀態
    /// </summary>
    private async Task CheckStreamScheduleStatusAsync(Video stream)
    {
        try
        {
            _logger.LogDebug("Checking schedule status for stream {VideoId} - {Title}", 
                stream.VideoId, stream.VideoTitle);
            
            // 使用 YouTube API 獲取最新狀態
            var videoData = await _youtubeApiService.GetVideoAsync(stream.VideoId);
            
            if (videoData == null)
            {
                _logger.LogWarning("Failed to retrieve data for scheduled stream {VideoId}", stream.VideoId);
                
                // 廣播錯誤事件
                await _eventService.BroadcastErrorEventAsync(
                    "VideoDataRetrievalError",
                    $"Failed to retrieve data for scheduled stream {stream.VideoId}",
                    new Dictionary<string, object>
                    {
                        ["VideoId"] = stream.VideoId,
                        ["Title"] = stream.VideoTitle ?? "Unknown"
                    });
                return;
            }

            // 檢查直播狀態變化
            var previousStatus = _previousStreamStates.ContainsKey(stream.VideoId) 
                ? _previousStreamStates[stream.VideoId] 
                : StreamStatus.Unknown;

            var currentStatus = DetermineStreamStatus(videoData);
            
            // 如果狀態有變化，記錄並廣播
            if (currentStatus != previousStatus)
            {
                _logger.LogInformation("Stream status changed for {VideoId}: {PreviousStatus} → {CurrentStatus}", 
                    stream.VideoId, previousStatus, currentStatus);

                // 更新狀態快取
                _previousStreamStates[stream.VideoId] = currentStatus;

                // 根據狀態變化廣播對應的事件
                await BroadcastStreamStatusChangeAsync(videoData, previousStatus, currentStatus);
            }
            else
            {
                // 即使狀態沒變，也發送影片更新事件（包含觀看人數等資訊）
                var videoInfo = YouTubeVideoInfo.FromGoogleVideo(videoData);
                await _eventService.BroadcastVideoUpdateAsync(videoInfo, "ScheduleCheck");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check schedule status for stream {VideoId}", stream.VideoId);
            
            await _eventService.BroadcastErrorEventAsync(
                "ScheduleCheckError",
                $"Failed to check schedule status for stream {stream.VideoId}: {ex.Message}",
                new Dictionary<string, object>
                {
                    ["VideoId"] = stream.VideoId,
                    ["ExceptionType"] = ex.GetType().Name
                });
        }
    }

    /// <summary>
    /// 檢查和更新 YouTube 頻道標題
    /// </summary>
    public async Task CheckChannelTitlesAsync()
    {
        try
        {
            _logger.LogInformation("Starting channel titles check");
            
            int updatedCount = 0;
            int totalCount = 0;

            using var db = _dbService.GetDbContext();
            
            var allChannels = await db.YoutubeChannelSpider.AsNoTracking().ToListAsync();
            var channelIdList = allChannels.Select(x => x.ChannelId)
                                          .Where(x => !string.IsNullOrEmpty(x))
                                          .Distinct()
                                          .ToList();
                                          
            totalCount = channelIdList.Count;
            _logger.LogInformation("Checking titles for {Count} channels", totalCount);

            const int chunkSize = 50; // YouTube API 每次最多查詢 50 個頻道
            
            for (int i = 0; i < channelIdList.Count; i += chunkSize)
            {
                var chunk = channelIdList.Skip(i).Take(chunkSize).ToList();
                
                try
                {
                    var channelTitles = await _youtubeApiService.GetChannelTitle(chunk);
                    
                    if (channelTitles?.Any() != true)
                    {
                        _logger.LogWarning("Failed to retrieve channel titles for chunk {ChunkStart}-{ChunkEnd}", 
                            i, Math.Min(i + chunkSize - 1, channelIdList.Count - 1));
                        continue;
                    }

                    foreach (var channelId in chunk)
                    {
                        var newTitle = channelTitles.FirstOrDefault(t => t.Contains(channelId));
                        if (!string.IsNullOrEmpty(newTitle))
                        {
                            var spider = await db.YoutubeChannelSpider
                                .FirstOrDefaultAsync(x => x.ChannelId == channelId);
                                
                            if (spider != null && spider.ChannelTitle != newTitle)
                            {
                                _logger.LogInformation("Updating channel title: {ChannelId} from '{OldTitle}' to '{NewTitle}'", 
                                    channelId, spider.ChannelTitle, newTitle);
                                    
                                spider.ChannelTitle = newTitle;
                                db.Update(spider);
                                updatedCount++;
                            }
                        }
                        else
                        {
                            _logger.LogWarning("No title found for channel {ChannelId}", channelId);
                        }
                    }
                    
                    await db.SaveChangesAsync();
                    
                    // 批次之間延遲以避免 API 配額過度使用
                    if (i + chunkSize < channelIdList.Count)
                    {
                        await Task.Delay(500);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing channel title chunk {ChunkStart}-{ChunkEnd}", 
                        i, Math.Min(i + chunkSize - 1, channelIdList.Count - 1));
                }
            }

            _logger.LogInformation("Channel title check completed: {Updated}/{Total} channels updated", 
                updatedCount, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in channel titles check");
        }
    }

    /// <summary>
    /// 根據 YouTube API 影片資料判斷直播狀態
    /// </summary>
    private StreamStatus DetermineStreamStatus(Google.Apis.YouTube.v3.Data.Video videoData)
    {
        if (videoData?.Snippet == null)
            return StreamStatus.Unknown;

        var liveBroadcastContent = videoData.Snippet.LiveBroadcastContent;
        
        return liveBroadcastContent switch
        {
            "live" => StreamStatus.Live,
            "upcoming" => StreamStatus.Scheduled,
            "none" when videoData.LiveStreamingDetails?.ActualEndTimeDateTimeOffset != null => StreamStatus.Ended,
            "none" => StreamStatus.Unknown,
            _ => StreamStatus.Unknown
        };
    }

    /// <summary>
    /// 廣播直播狀態變化事件
    /// </summary>
    private async Task BroadcastStreamStatusChangeAsync(
        Google.Apis.YouTube.v3.Data.Video videoData, 
        StreamStatus previousStatus, 
        StreamStatus currentStatus)
    {
        try
        {
            var videoInfo = YouTubeVideoInfo.FromGoogleVideo(videoData);

            // 根據狀態變化決定廣播的事件類型
            switch (currentStatus)
            {
                case StreamStatus.Live when previousStatus != StreamStatus.Live:
                    await _eventService.BroadcastStreamStartAsync(videoInfo);
                    break;

                case StreamStatus.Ended when previousStatus == StreamStatus.Live:
                    await _eventService.BroadcastStreamEndAsync(videoInfo);
                    break;

                case StreamStatus.Scheduled when previousStatus != StreamStatus.Scheduled:
                    await _eventService.BroadcastVideoUpdateAsync(videoInfo, "ScheduledStatusChange");
                    break;

                default:
                    // 其他狀態變化作為一般影片更新處理
                    await _eventService.BroadcastVideoUpdateAsync(videoInfo, $"StatusChange:{previousStatus}→{currentStatus}");
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast stream status change for video {VideoId}", videoData.Id);
        }
    }
}

/// <summary>
/// 直播狀態枚舉
/// </summary>
public enum StreamStatus
{
    Unknown,
    Scheduled,
    Live,
    Ended,
    Cancelled,
    Private
}

/// <summary>
/// 直播狀態變化資訊
/// </summary>
public class StreamStatusChange
{
    public string VideoId { get; set; } = string.Empty;
    public string ChannelId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public StreamStatus PreviousStatus { get; set; }
    public StreamStatus CurrentStatus { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime ChangeDetectedAt { get; set; } = DateTime.UtcNow;
}
