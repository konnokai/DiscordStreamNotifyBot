using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StreamNotifyBot.Crawler.Configuration;
using System.Collections.Concurrent;

namespace StreamNotifyBot.Crawler.Services;

/// <summary>
/// 直播追蹤管理服務實作
/// </summary>
public class StreamTrackerService : IStreamTracker
{
    private readonly ILogger<StreamTrackerService> _logger;
    private readonly CrawlerConfig _config;

    // 全域追蹤計數器：StreamKey -> GuildIds
    private readonly ConcurrentDictionary<string, ConcurrentHashSet<ulong>> _globalTrackingCounter = new();
    private readonly object _trackingLock = new object();

    public StreamTrackerService(
        ILogger<StreamTrackerService> logger,
        IOptions<CrawlerConfig> config)
    {
        _logger = logger;
        _config = config.Value;
    }

    public async Task<bool> AddTrackingAsync(string platform, string streamKey, ulong guildId, ulong channelId, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = CreateStreamKey(platform, streamKey);

            lock (_trackingLock)
            {
                if (_globalTrackingCounter.TryGetValue(key, out var guilds))
                {
                    guilds.Add(guildId);
                    _logger.LogDebug("Added guild {GuildId} to existing tracking for {Platform}:{StreamKey}",
                        guildId, platform, streamKey);
                }
                else
                {
                    var newGuilds = new ConcurrentHashSet<ulong>();
                    newGuilds.Add(guildId);
                    _globalTrackingCounter[key] = newGuilds;
                    _logger.LogInformation("Started new tracking for {Platform}:{StreamKey} with guild {GuildId}",
                        platform, streamKey, guildId);
                }
            }

            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding tracking for {Platform}:{StreamKey} in guild {GuildId}",
                platform, streamKey, guildId);
            return false;
        }
    }

    public async Task<bool> RemoveTrackingAsync(string platform, string streamKey, ulong guildId, ulong channelId, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = CreateStreamKey(platform, streamKey);

            lock (_trackingLock)
            {
                if (_globalTrackingCounter.TryGetValue(key, out var guilds))
                {
                    guilds.Remove(guildId);

                    if (guilds.Count == 0)
                    {
                        _globalTrackingCounter.TryRemove(key, out _);
                        _logger.LogInformation("Stopped tracking for {Platform}:{StreamKey} - no more guilds tracking",
                            platform, streamKey);
                    }
                    else
                    {
                        _logger.LogDebug("Removed guild {GuildId} from tracking for {Platform}:{StreamKey}",
                            guildId, platform, streamKey);
                    }

                    return true;
                }
            }

            _logger.LogWarning("Attempted to remove non-existent tracking for {Platform}:{StreamKey} in guild {GuildId}",
                platform, streamKey, guildId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing tracking for {Platform}:{StreamKey} in guild {GuildId}",
                platform, streamKey, guildId);
            return false;
        }
    }

    public async Task<IReadOnlyList<ulong>> GetTrackingGuildsAsync(string platform, string streamKey, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = CreateStreamKey(platform, streamKey);

            if (_globalTrackingCounter.TryGetValue(key, out var guilds))
            {
                return await Task.FromResult(guilds.ToList().AsReadOnly());
            }

            return await Task.FromResult(new List<ulong>().AsReadOnly());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tracking guilds for {Platform}:{StreamKey}", platform, streamKey);
            return new List<ulong>().AsReadOnly();
        }
    }

    public async Task<TrackingStatistics> GetTrackingStatisticsAsync()
    {
        try
        {
            var statistics = new TrackingStatistics();

            lock (_trackingLock)
            {
                statistics.TotalTrackingCount = _globalTrackingCounter.Count;

                // 統計各平台追蹤數
                foreach (var kvp in _globalTrackingCounter)
                {
                    var platform = ExtractPlatformFromKey(kvp.Key);
                    if (!statistics.PlatformTrackingCounts.ContainsKey(platform))
                    {
                        statistics.PlatformTrackingCounts[platform] = 0;
                    }
                    statistics.PlatformTrackingCounts[platform]++;
                }

                // 統計活躍 Guild 數量
                var allGuilds = new HashSet<ulong>();
                foreach (var guilds in _globalTrackingCounter.Values)
                {
                    foreach (var guildId in guilds)
                    {
                        allGuilds.Add(guildId);
                    }
                }
                statistics.ActiveGuildCount = allGuilds.Count;
            }

            return await Task.FromResult(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tracking statistics");
            return new TrackingStatistics();
        }
    }

    public async Task LoadTrackingDataAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Loading tracking data from database...");

        try
        {
            // 這裡會在後續 Story 中實作從資料庫載入追蹤資料的邏輯
            // 目前暫時保持空實作
            await Task.CompletedTask;

            _logger.LogInformation("Tracking data loaded successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading tracking data from database");
            throw;
        }
    }

    private static string CreateStreamKey(string platform, string streamKey)
    {
        return $"{platform.ToLowerInvariant()}:{streamKey}";
    }

    private static string ExtractPlatformFromKey(string key)
    {
        var colonIndex = key.IndexOf(':');
        return colonIndex > 0 ? key.Substring(0, colonIndex) : key;
    }
}

/// <summary>
/// 執行緒安全的 HashSet 實作
/// </summary>
/// <typeparam name="T">元素類型</typeparam>
public class ConcurrentHashSet<T> : IDisposable, IEnumerable<T> where T : notnull
{
    private readonly HashSet<T> _hashSet = new();
    private readonly ReaderWriterLockSlim _lock = new();

    public int Count
    {
        get
        {
            _lock.EnterReadLock();
            try
            {
                return _hashSet.Count;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
    }

    public bool Add(T item)
    {
        _lock.EnterWriteLock();
        try
        {
            return _hashSet.Add(item);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public bool Remove(T item)
    {
        _lock.EnterWriteLock();
        try
        {
            return _hashSet.Remove(item);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public bool Contains(T item)
    {
        _lock.EnterReadLock();
        try
        {
            return _hashSet.Contains(item);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public List<T> ToList()
    {
        _lock.EnterReadLock();
        try
        {
            return new List<T>(_hashSet);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public void Clear()
    {
        _lock.EnterWriteLock();
        try
        {
            _hashSet.Clear();
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public IEnumerator<T> GetEnumerator()
    {
        _lock.EnterReadLock();
        try
        {
            return new List<T>(_hashSet).GetEnumerator();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Dispose()
    {
        _lock.Dispose();
        GC.SuppressFinalize(this);
    }
}
