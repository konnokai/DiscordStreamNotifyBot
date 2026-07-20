using System.Globalization;
using System.Net.Http;
using System.Text;

internal sealed class LokiLogSink
{
    private const int QueueCapacity = 10_000;
    private const int BatchSize = 100;
    private static readonly TimeSpan BatchInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan MaxRetryDelay = TimeSpan.FromSeconds(30);

    private readonly Uri _endpoint;
    private readonly Action<string> _writeDiagnostic;
    private readonly HttpClient _httpClient = new() { Timeout = Timeout.InfiniteTimeSpan };
    private readonly Queue<LokiLogEntry> _queue = new();
    private readonly object _queueLock = new();
    private readonly SemaphoreSlim _signal = new(0, 1);
    private readonly CancellationTokenSource _abort = new();
    private readonly Task _worker;
    private long _lastTimestampNanoseconds;
    private int _inFlightCount;
    private int _stopping;
    private int _failureReported;
    private int _rejectionReported;
    private int _overflowReported;
    private int _disposed;

    public LokiLogSink(Uri endpoint, Action<string> writeDiagnostic)
    {
        _endpoint = endpoint;
        _writeDiagnostic = writeDiagnostic;
        _worker = Task.Run(RunAsync);
    }

    public void TryEnqueue(Log.LogLevel level, string role, string line)
    {
        if (Volatile.Read(ref _stopping) != 0)
            return;

        bool queueWasEmpty;
        bool queueOverflowed = false;
        lock (_queueLock)
        {
            if (_stopping != 0)
                return;

            queueWasEmpty = _queue.Count == 0;
            if (_queue.Count >= QueueCapacity)
            {
                _queue.Dequeue();
                queueOverflowed = true;
            }

            _queue.Enqueue(new LokiLogEntry(
                NextTimestampNanoseconds(),
                NormalizeRole(role),
                level.ToString().ToUpperInvariant(),
                line));
        }

        if (queueWasEmpty)
            SignalWorker();

        if (queueOverflowed && Interlocked.Exchange(ref _overflowReported, 1) == 0)
            ReportDiagnostic($"Loki 等待佇列已滿（{QueueCapacity} 筆），開始淘汰最舊的 Loki 副本；console log 不受影響");
    }

    public async Task StopAsync(TimeSpan timeout)
    {
        if (Interlocked.Exchange(ref _stopping, 1) == 0)
            SignalWorker();

        using var timeoutCts = new CancellationTokenSource(timeout);
        try
        {
            await _worker.WaitAsync(timeoutCts.Token);
        }
        catch (OperationCanceledException)
        {
            int pendingCount = GetPendingCount();
            _abort.Cancel();
            SignalWorker();

            if (pendingCount > 0)
                ReportDiagnostic($"Loki 關閉前推送逾時，仍有 {pendingCount} 筆僅保留於 console/Docker log");

            // timeout 是程序關閉的硬上限；worker 會透過 _abort 結束，不再同步等待。
            _ = _worker.ContinueWith(
                _ => DisposeResources(),
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
            return;
        }
        finally
        {
            if (_worker.IsCompleted)
                DisposeResources();
        }
    }

    private async Task RunAsync()
    {
        try
        {
            while (true)
            {
                if (!TryDequeueBatch(out List<LokiLogEntry> batch))
                {
                    if (Volatile.Read(ref _stopping) != 0)
                        return;

                    await _signal.WaitAsync(_abort.Token);
                    if (Volatile.Read(ref _stopping) == 0)
                        await Task.Delay(BatchInterval, _abort.Token);
                    continue;
                }

                try
                {
                    await SendWithRetryAsync(batch, _abort.Token);
                }
                finally
                {
                    lock (_queueLock)
                        _inFlightCount = 0;
                }
                if (GetQueueCount() < QueueCapacity)
                    Volatile.Write(ref _overflowReported, 0);
            }
        }
        catch (OperationCanceledException) when (_abort.IsCancellationRequested) { }
        catch (Exception ex)
        {
            ReportDiagnostic($"Loki 背景推送程序意外停止：{ex.Demystify()}");
        }
    }

    private async Task SendWithRetryAsync(List<LokiLogEntry> batch, CancellationToken cancellationToken)
    {
        string payload = BuildPayload(batch);
        int retryCount = 0;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, _endpoint)
                {
                    Content = new StringContent(payload, Encoding.UTF8, "application/json")
                };
                using var requestCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                requestCts.CancelAfter(RequestTimeout);
                using var response = await _httpClient.SendAsync(request, requestCts.Token);
                if (!response.IsSuccessStatusCode)
                {
                    int statusCode = (int)response.StatusCode;
                    if (statusCode == 408 || statusCode == 429 || statusCode >= 500)
                        throw new HttpRequestException($"Loki 回傳 HTTP {statusCode} {response.ReasonPhrase}");

                    if (Interlocked.Exchange(ref _rejectionReported, 1) == 0)
                        ReportDiagnostic($"Loki 拒絕寫入（HTTP {statusCode} {response.ReasonPhrase}），已丟棄此批 {batch.Count} 筆並繼續後續推送");
                    Interlocked.Exchange(ref _failureReported, 0);
                    return;
                }

                if (Interlocked.Exchange(ref _failureReported, 0) != 0)
                    ReportDiagnostic("Loki 推送已恢復");
                Interlocked.Exchange(ref _rejectionReported, 0);
                return;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                if (Interlocked.Exchange(ref _failureReported, 1) == 0)
                    ReportDiagnostic($"Loki 推送失敗，將於背景重試：{ex.Demystify().Message}");

                retryCount++;
                double retrySeconds = Math.Min(Math.Pow(2, Math.Min(retryCount - 1, 10)), MaxRetryDelay.TotalSeconds);
                await Task.Delay(TimeSpan.FromSeconds(retrySeconds), cancellationToken);
            }
        }
    }

    private bool TryDequeueBatch(out List<LokiLogEntry> batch)
    {
        lock (_queueLock)
        {
            if (_queue.Count == 0)
            {
                batch = null;
                return false;
            }

            int count = Math.Min(BatchSize, _queue.Count);
            batch = new List<LokiLogEntry>(count);
            for (int i = 0; i < count; i++)
                batch.Add(_queue.Dequeue());
            _inFlightCount = count;
            return true;
        }
    }

    private int GetQueueCount()
    {
        lock (_queueLock)
            return _queue.Count;
    }

    private int GetPendingCount()
    {
        lock (_queueLock)
            return _queue.Count + _inFlightCount;
    }

    private void SignalWorker()
    {
        try
        {
            if (_signal.CurrentCount == 0)
                _signal.Release();
        }
        catch (ObjectDisposedException) { }
        catch (SemaphoreFullException) { }
    }

    private void ReportDiagnostic(string message)
    {
        _ = Task.Run(() =>
        {
            try { _writeDiagnostic(message); }
            catch { }
        });
    }

    private void DisposeResources()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        _abort.Cancel();
        _httpClient.Dispose();
        _signal.Dispose();
        _abort.Dispose();
    }

    private long NextTimestampNanoseconds()
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1_000_000;
        while (true)
        {
            long previous = Volatile.Read(ref _lastTimestampNanoseconds);
            long next = Math.Max(now, previous + 1);
            if (Interlocked.CompareExchange(ref _lastTimestampNanoseconds, next, previous) == previous)
                return next;
        }
    }

    private static string BuildPayload(List<LokiLogEntry> entries)
    {
        var streams = entries
            .GroupBy(x => new { x.Role, x.Level })
            .Select(group => new
            {
                stream = new Dictionary<string, string>
                {
                    ["app"] = "discord-stream-notify-bot",
                    ["service"] = GetServiceName(group.Key.Role),
                    ["role"] = group.Key.Role,
                    ["level"] = group.Key.Level
                },
                values = group.Select(x => new[]
                {
                    x.TimestampNanoseconds.ToString(CultureInfo.InvariantCulture),
                    x.Line
                }).ToArray()
            }).ToArray();

        return JsonConvert.SerializeObject(new { streams });
    }

    private static string NormalizeRole(string role)
        => string.IsNullOrWhiteSpace(role) ? "unknown" : role;

    private static string GetServiceName(string role)
    {
        int separatorIndex = role.IndexOf(':');
        return separatorIndex > 0 ? role[..separatorIndex] : role;
    }

    private sealed record LokiLogEntry(long TimestampNanoseconds, string Role, string Level, string Line);
}
