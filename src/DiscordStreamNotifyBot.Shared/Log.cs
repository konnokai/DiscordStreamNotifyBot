using System.Net;
using Serilog;
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Sinks.Grafana.Loki;

public static class Log
{
    public enum LogLevel { Trace, Debug, Info, Warn, Error, Critical }

    private enum LogFileRoute { None, General, Stream, Error }

    private const string LokiPushPath = "/loki/api/v1/push";
    private const string FileRouteProperty = "FileRoute";
    private const string WriteConsoleProperty = "WriteConsole";
    private const string ConsoleErrorProperty = "ConsoleError";
    private const string RoleDisplayProperty = "RoleDisplay";
    private const string LevelNameProperty = "LevelName";
    private const string LokiLevelProperty = "level";
    private static readonly ITextFormatter ConsoleFormatter = new LogTextFormatter(useColor: true);
    private static readonly ITextFormatter FileFormatter = new LogTextFormatter(useColor: false);
    private static readonly string LogFilePrefix = DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss");
    private static readonly object LoggerLock = new();
    private static Logger _logger = CreateBootstrapLogger();
    private static Task _shutdownTask;
    private static long _shutdownDeadline;
    private static int _shutdownTimeoutReported;
    private static string _rolePrefix = "";
    private static bool _configured;

    static Log()
    {
        SelfLog.Enable(message =>
        {
            try { Console.Error.Write(message); }
            catch { }
        });

        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            try { ShutdownAsync(TimeSpan.FromSeconds(2)).GetAwaiter().GetResult(); }
            catch { }
        };
    }

    /// <summary>多程序部署的角色標籤（如 <c>scraper</c> / <c>notifier:0</c> / <c>coordinator</c>），跨程序追蹤用。空字串則不加。</summary>
    public static string RolePrefix
    {
        get => Volatile.Read(ref _rolePrefix);
        set => Volatile.Write(ref _rolePrefix, value ?? "");
    }

    /// <summary>從完整 log 行擷取 timestamp、role、level、message 的 .NET regex。</summary>
    public const string LogLinePattern = @"^\[(?<timestamp>\d{4}/\d{2}/\d{2} \d{2}:\d{2}:\d{2})\] (?:\[(?<role>[^\]]+)\] )?\[(?<level>TRACE|DEBUG|INFO|WARN|ERROR|CRITICAL)\] \| (?<message>.*)$";

    /// <summary>只偵測標準 log level 的 regex，供不解析完整行的收集器使用。</summary>
    public const string LogLevelPattern = @"\[(?<level>TRACE|DEBUG|INFO|WARN|ERROR|CRITICAL)\]";

    /// <summary>Docker／Container 環境只輸出 stdout/stderr，不在暫存檔案系統寫入 Log。</summary>
    public static bool IsRunningInContainer { get; } =
        IsTrueEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") ||
        IsTrueEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINERS");

    public static void New(string text, bool newLine = true)
        => WriteText(text, newLine, false, LogLevel.Info, LogFileRoute.Stream);

    public static void New(string messageTemplate, object propertyValue, params object[] additionalPropertyValues)
        => WriteTemplate(messageTemplate, CombinePropertyValues(propertyValue, additionalPropertyValues), false,
            LogLevel.Info, LogFileRoute.Stream);

    public static void Debug(string text, bool newLine = true)
    {
        if (!Debugger.IsAttached)
            return;

        WriteText(text, newLine, false, LogLevel.Debug, LogFileRoute.None);
    }

    public static void Debug(string messageTemplate, object propertyValue, params object[] additionalPropertyValues)
    {
        if (!Debugger.IsAttached)
            return;

        WriteTemplate(messageTemplate, CombinePropertyValues(propertyValue, additionalPropertyValues), false,
            LogLevel.Debug, LogFileRoute.None);
    }

    public static void Info(string text, bool newLine = true)
        => WriteText(text, newLine, false, LogLevel.Info, LogFileRoute.General);

    public static void Info(string messageTemplate, object propertyValue, params object[] additionalPropertyValues)
        => WriteTemplate(messageTemplate, CombinePropertyValues(propertyValue, additionalPropertyValues), false,
            LogLevel.Info, LogFileRoute.General);

    public static void Warn(string text, bool newLine = true)
        => WriteText(text, newLine, false, LogLevel.Warn, LogFileRoute.General);

    public static void Warn(string messageTemplate, object propertyValue, params object[] additionalPropertyValues)
        => WriteTemplate(messageTemplate, CombinePropertyValues(propertyValue, additionalPropertyValues), false,
            LogLevel.Warn, LogFileRoute.General);

    public static void Error(string text, bool newLine = true, bool writeLog = true)
        => WriteText(text, newLine, true, LogLevel.Error,
            writeLog ? LogFileRoute.Error : LogFileRoute.None);

    public static void Error(string messageTemplate, object propertyValue, params object[] additionalPropertyValues)
        => WriteTemplate(messageTemplate, CombinePropertyValues(propertyValue, additionalPropertyValues), true,
            LogLevel.Error, LogFileRoute.Error);

    public static void Error(Exception ex, string text, bool newLine = true, bool writeLog = true)
        => WriteText(text, newLine, true, LogLevel.Error,
            writeLog ? LogFileRoute.Error : LogFileRoute.None, exception: ex?.Demystify());

    public static void Error(Exception ex, string messageTemplate, object propertyValue,
        params object[] additionalPropertyValues)
        => WriteTemplate(messageTemplate, CombinePropertyValues(propertyValue, additionalPropertyValues), true,
            LogLevel.Error, LogFileRoute.Error, ex?.Demystify());

    public static void FormatColorWrite(string text, ConsoleColor consoleColor = ConsoleColor.Gray,
        bool newLine = true, bool isError = false, LogLevel level = LogLevel.Info)
    {
        _ = consoleColor;
        WriteText(text, newLine, isError, level, LogFileRoute.None);
    }

    /// <summary>建立完整 Serilog pipeline。未設定或 URL 無效時保留 console/file 行為。</summary>
    public static void ConfigureLoki(string url)
    {
        string lokiBaseUrl = null;
        bool lokiEnabled = false;
        if (!string.IsNullOrWhiteSpace(url))
        {
            if (TryNormalizeLokiBaseUrl(url, out lokiBaseUrl))
            {
                lokiEnabled = true;
            }
            else
            {
                WriteLokiDiagnostic("LOKI_URL 無效，略過 Loki 主動推送");
            }
        }

        Logger previousLogger;
        lock (LoggerLock)
        {
            if (_configured || _shutdownTask != null)
                return;

            Logger replacement;
            try
            {
                replacement = CreateFullLogger(lokiBaseUrl);
            }
            catch (Exception ex)
            {
                lokiEnabled = false;
                WriteLokiDiagnostic($"Serilog Loki sink 初始化失敗，改用 console/file：{ex.Demystify().Message}");
                replacement = CreateFullLogger(null);
            }

            previousLogger = _logger;
            _logger = replacement;
            _configured = true;
        }

        previousLogger.Dispose();
        if (lokiEnabled)
            Info("Loki 主動推送已啟用");
    }

    /// <summary>在指定時間內 best-effort flush Serilog sinks；逾時後不阻塞程序關閉。</summary>
    public static async Task ShutdownAsync(TimeSpan timeout)
    {
        Task shutdownTask;
        lock (LoggerLock)
        {
            if (_shutdownTask == null)
            {
                Logger logger = _logger;
                _logger = new LoggerConfiguration().CreateLogger();
                _shutdownDeadline = GetShutdownDeadline(timeout);
                _shutdownTask = Task.Run(logger.Dispose);
            }

            shutdownTask = _shutdownTask;
        }

        TimeSpan waitTimeout = GetRemainingShutdownTime(timeout);
        if (waitTimeout <= TimeSpan.Zero)
        {
            ReportShutdownTimeout(waitTimeout);
            return;
        }

        try
        {
            await shutdownTask.WaitAsync(waitTimeout);
        }
        catch (TimeoutException)
        {
            ReportShutdownTimeout(waitTimeout);
        }
        catch (Exception ex)
        {
            WriteLokiDiagnostic($"Serilog 關閉失敗：{ex.Demystify().Message}");
        }
    }

    public static Task LogMsg(LogMessage message)
    {
        LogLevel level = message.Severity switch
        {
            LogSeverity.Critical => LogLevel.Critical,
            LogSeverity.Error => LogLevel.Error,
            LogSeverity.Warning => LogLevel.Warn,
            LogSeverity.Debug => LogLevel.Debug,
            LogSeverity.Verbose => LogLevel.Trace,
            _ => LogLevel.Info
        };

        bool includeException = message.Exception != null &&
            message.Message != null &&
            !message.Message.Contains("TYPING_START") &&
            message.Exception is not GatewayReconnectException &&
            message.Exception is not TaskCanceledException &&
            message.Exception is not JsonSerializationException &&
            message.Exception is not NullReferenceException;

#if DEBUG || DEBUG_DONTREGISTERCOMMAND
        LogFileRoute fileRoute = LogFileRoute.None;
        bool writeConsole = true;
#else
        LogFileRoute fileRoute = IsRunningInContainer ? LogFileRoute.None : LogFileRoute.General;
        bool writeConsole = IsRunningInContainer;
#endif

        if (includeException)
        {
            LogLevel exceptionLevel = level is LogLevel.Error or LogLevel.Critical ? level : LogLevel.Error;
            WriteText(message.Message, true, true, exceptionLevel, fileRoute, true, message.Exception.Demystify());
        }
        else if (!string.IsNullOrEmpty(message.Message))
        {
            WriteText(message.Message, true, false, level, fileRoute, writeConsole);
        }

        return Task.CompletedTask;
    }

    private static Logger CreateBootstrapLogger()
    {
        var configuration = new LoggerConfiguration().MinimumLevel.Verbose();
        ConfigureConsole(configuration);
        return configuration.CreateLogger();
    }

    private static Logger CreateFullLogger(string lokiBaseUrl)
    {
        var configuration = new LoggerConfiguration().MinimumLevel.Verbose();
        ConfigureConsole(configuration);

        if (!IsRunningInContainer && !Debugger.IsAttached)
            ConfigureFiles(configuration);

        if (lokiBaseUrl != null)
        {
            string role = NormalizeRole(RolePrefix);
            string service = GetServiceName(role);
            configuration.WriteTo.GrafanaLoki(
                lokiBaseUrl,
                labels: new[]
                {
                    new LokiLabel { Key = "app", Value = "discord-stream-notify-bot" },
                    new LokiLabel { Key = "service", Value = service },
                    new LokiLabel { Key = "role", Value = role }
                },
                propertiesAsLabels: new[] { LokiLevelProperty },
                handleLogLevelAsLabel: false,
                batchSizeLimit: 100,
                queueLimit: 10_000,
                period: TimeSpan.FromSeconds(1),
                eagerlyEmitFirstEvent: false,
                retryTimeLimit: TimeSpan.FromMinutes(10),
                httpMessageHandler: new LokiHttpMessageHandler(TimeSpan.FromSeconds(5)));
        }

        return configuration.CreateLogger();
    }

    private static void ConfigureConsole(LoggerConfiguration configuration)
    {
        configuration.WriteTo.Logger(logger => logger
            .Filter.ByIncludingOnly(IsStandardOutputEvent)
            .WriteTo.Console(ConsoleFormatter));
        configuration.WriteTo.Logger(logger => logger
            .Filter.ByIncludingOnly(IsStandardErrorEvent)
            .WriteTo.Console(ConsoleFormatter, standardErrorFromLevel: LogEventLevel.Verbose));
    }

    private static void ConfigureFiles(LoggerConfiguration configuration)
    {
        configuration.WriteTo.Logger(logger => logger
            .Filter.ByIncludingOnly(IsGeneralFileEvent)
            .WriteTo.Sink(new DeferredFileSink($"{LogFilePrefix}.log", FileFormatter)));
        configuration.WriteTo.Logger(logger => logger
            .Filter.ByIncludingOnly(e => IsFileEvent(e, LogFileRoute.Error))
            .WriteTo.Sink(new DeferredFileSink($"{LogFilePrefix}_err.log", FileFormatter)));
        configuration.WriteTo.Logger(logger => logger
            .Filter.ByIncludingOnly(e => IsFileEvent(e, LogFileRoute.Stream))
            .WriteTo.Sink(new DeferredFileSink($"{LogFilePrefix}_stream.log", FileFormatter)));
    }

    private static void WriteText(string text, bool newLine, bool consoleError, LogLevel level,
        LogFileRoute fileRoute, bool writeConsole = true, Exception exception = null)
    {
        _ = newLine;
        WriteTemplate("{LogText:l}", new object[] { text ?? "" }, consoleError, level, fileRoute,
            exception, writeConsole);
    }

    private static void WriteTemplate(string messageTemplate, object[] propertyValues, bool consoleError,
        LogLevel level, LogFileRoute fileRoute, Exception exception = null, bool writeConsole = true)
    {
        string role = RolePrefix;
        string roleDisplay = string.IsNullOrWhiteSpace(role) ? "" : $"[{role}] ";
        string levelName = GetLevelName(level);

        lock (LoggerLock)
        {
            _logger
                .ForContext(FileRouteProperty, fileRoute.ToString())
                .ForContext(WriteConsoleProperty, writeConsole)
                .ForContext(ConsoleErrorProperty, consoleError)
                .ForContext(RoleDisplayProperty, roleDisplay)
                .ForContext(LevelNameProperty, levelName)
                .ForContext(LokiLevelProperty, levelName)
                .Write(ToSerilogLevel(level), exception, messageTemplate ?? "", propertyValues ?? Array.Empty<object>());
        }
    }

    private static bool IsStandardOutputEvent(LogEvent logEvent)
        => GetBooleanProperty(logEvent, WriteConsoleProperty, true) &&
           !GetBooleanProperty(logEvent, ConsoleErrorProperty, false) &&
           logEvent.Level < LogEventLevel.Error;

    private static bool IsStandardErrorEvent(LogEvent logEvent)
        => GetBooleanProperty(logEvent, WriteConsoleProperty, true) &&
           (GetBooleanProperty(logEvent, ConsoleErrorProperty, false) || logEvent.Level >= LogEventLevel.Error);

    private static bool IsGeneralFileEvent(LogEvent logEvent)
    {
        if (Debugger.IsAttached)
            return false;

        string route = GetStringProperty(logEvent, FileRouteProperty);
        return route == nameof(LogFileRoute.General) ||
               route == nameof(LogFileRoute.Error) ||
               route == nameof(LogFileRoute.Stream);
    }

    private static bool IsFileEvent(LogEvent logEvent, LogFileRoute route)
        => !Debugger.IsAttached && GetStringProperty(logEvent, FileRouteProperty) == route.ToString();

    private static bool GetBooleanProperty(LogEvent logEvent, string name, bool defaultValue)
        => logEvent.Properties.TryGetValue(name, out LogEventPropertyValue value) &&
           value is ScalarValue { Value: bool boolean }
            ? boolean
            : defaultValue;

    private static string GetStringProperty(LogEvent logEvent, string name)
        => logEvent.Properties.TryGetValue(name, out LogEventPropertyValue value) &&
           value is ScalarValue { Value: string text }
            ? text
            : null;

    private static object[] CombinePropertyValues(object propertyValue, object[] additionalPropertyValues)
    {
        var values = new object[(additionalPropertyValues?.Length ?? 0) + 1];
        values[0] = propertyValue;
        if (additionalPropertyValues?.Length > 0)
            Array.Copy(additionalPropertyValues, 0, values, 1, additionalPropertyValues.Length);
        return values;
    }

    private static long GetShutdownDeadline(TimeSpan timeout)
    {
        if (timeout == Timeout.InfiniteTimeSpan)
            return long.MaxValue;

        double timeoutTicks = Math.Max(0, timeout.TotalSeconds) * Stopwatch.Frequency;
        return Stopwatch.GetTimestamp() + (long)timeoutTicks;
    }

    private static TimeSpan GetRemainingShutdownTime(TimeSpan requestedTimeout)
    {
        if (_shutdownDeadline == long.MaxValue)
            return requestedTimeout;

        double remainingSeconds = (_shutdownDeadline - Stopwatch.GetTimestamp()) / (double)Stopwatch.Frequency;
        if (remainingSeconds <= 0)
            return TimeSpan.Zero;

        TimeSpan remaining = TimeSpan.FromSeconds(remainingSeconds);
        if (requestedTimeout == Timeout.InfiniteTimeSpan)
            return remaining;
        return requestedTimeout < remaining ? requestedTimeout : remaining;
    }

    private static void ReportShutdownTimeout(TimeSpan waitTimeout)
    {
        if (Interlocked.Exchange(ref _shutdownTimeoutReported, 1) == 0)
            WriteLokiDiagnostic($"Serilog 關閉前 flush 逾時（本次最多等待 {Math.Max(0, waitTimeout.TotalSeconds):0.###} 秒），剩餘事件僅保留於既有 console/Docker log");
    }

    private static bool TryNormalizeLokiBaseUrl(string value, out string baseUrl)
    {
        baseUrl = null;
        if (!Uri.TryCreate(value, UriKind.Absolute, out Uri endpoint) ||
            (endpoint.Scheme != Uri.UriSchemeHttp && endpoint.Scheme != Uri.UriSchemeHttps) ||
            !string.IsNullOrEmpty(endpoint.Query) ||
            !string.IsNullOrEmpty(endpoint.Fragment))
        {
            return false;
        }

        var builder = new UriBuilder(endpoint);
        string path = builder.Path.TrimEnd('/');
        if (path.EndsWith(LokiPushPath, StringComparison.OrdinalIgnoreCase))
        {
            path = path[..^LokiPushPath.Length].TrimEnd('/');
            builder.Path = string.IsNullOrEmpty(path) ? "/" : path;
        }

        baseUrl = builder.Uri.AbsoluteUri.TrimEnd('/');
        return true;
    }

    private static string NormalizeRole(string role)
        => string.IsNullOrWhiteSpace(role) ? "unknown" : role;

    private static string GetServiceName(string role)
    {
        int separatorIndex = role.IndexOf(':');
        return separatorIndex > 0 ? role[..separatorIndex] : role;
    }

    private static string GetLevelName(LogLevel level) => level switch
    {
        LogLevel.Trace => "TRACE",
        LogLevel.Debug => "DEBUG",
        LogLevel.Info => "INFO",
        LogLevel.Warn => "WARN",
        LogLevel.Error => "ERROR",
        LogLevel.Critical => "CRITICAL",
        _ => "INFO"
    };

    private static LogEventLevel ToSerilogLevel(LogLevel level) => level switch
    {
        LogLevel.Trace => LogEventLevel.Verbose,
        LogLevel.Debug => LogEventLevel.Debug,
        LogLevel.Info => LogEventLevel.Information,
        LogLevel.Warn => LogEventLevel.Warning,
        LogLevel.Error => LogEventLevel.Error,
        LogLevel.Critical => LogEventLevel.Fatal,
        _ => LogEventLevel.Information
    };

    private static bool IsTrueEnvironmentVariable(string name)
    {
        string value = Environment.GetEnvironmentVariable(name);
        return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) || value == "1";
    }

    private static void WriteLokiDiagnostic(string text)
    {
        try
        {
            string role = RolePrefix;
            string roleTag = string.IsNullOrWhiteSpace(role) ? "" : $"[{role}] ";
            Console.Error.WriteLine($"[{DateTime.Now:yyyy/MM/dd HH:mm:ss}] {roleTag}[WARN] | {text}");
        }
        catch { }
    }

    private sealed class LokiHttpMessageHandler : DelegatingHandler
    {
        private readonly TimeSpan _requestTimeout;
        private int _failureReported;
        private int _rejectionReported;

        public LokiHttpMessageHandler(TimeSpan requestTimeout)
            : base(new HttpClientHandler())
        {
            _requestTimeout = requestTimeout;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(_requestTimeout);

            try
            {
                HttpResponseMessage response = await base.SendAsync(request, timeoutCts.Token);
                int statusCode = (int)response.StatusCode;
                if (statusCode >= 400 && statusCode < 500 &&
                    response.StatusCode is not HttpStatusCode.RequestTimeout and not HttpStatusCode.TooManyRequests)
                {
                    response.Dispose();
                    if (Interlocked.Exchange(ref _rejectionReported, 1) == 0)
                        WriteLokiDiagnostic($"Loki 拒絕寫入（HTTP {statusCode}），已丟棄此批並繼續後續推送");
                    Interlocked.Exchange(ref _failureReported, 0);
                    return new HttpResponseMessage(HttpStatusCode.NoContent) { RequestMessage = request };
                }

                if (!response.IsSuccessStatusCode)
                    ReportFailure($"Loki 推送失敗（HTTP {statusCode}），將由 Serilog 於背景重試");
                else if (Interlocked.Exchange(ref _failureReported, 0) != 0)
                    WriteLokiDiagnostic("Loki 推送已恢復");

                if (response.IsSuccessStatusCode)
                    Interlocked.Exchange(ref _rejectionReported, 0);

                return response;
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                ReportFailure($"Loki 推送逾時（{_requestTimeout.TotalSeconds:0.###} 秒），將由 Serilog 於背景重試");
                throw;
            }
            catch (Exception ex)
            {
                ReportFailure($"Loki 推送失敗，將由 Serilog 於背景重試：{ex.Demystify().Message}");
                throw;
            }
        }

        private void ReportFailure(string message)
        {
            if (Interlocked.Exchange(ref _failureReported, 1) == 0)
                WriteLokiDiagnostic(message);
        }
    }

    private sealed class DeferredFileSink : ILogEventSink, IDisposable
    {
        private readonly string _path;
        private readonly ITextFormatter _formatter;
        private readonly object _lock = new();
        private Logger _fileLogger;
        private bool _disposed;

        public DeferredFileSink(string path, ITextFormatter formatter)
        {
            _path = path;
            _formatter = formatter;
        }

        public void Emit(LogEvent logEvent)
        {
            lock (_lock)
            {
                if (_disposed)
                    return;

                _fileLogger ??= new LoggerConfiguration()
                    .MinimumLevel.Verbose()
                    .WriteTo.File(
                        _formatter,
                        _path,
                        fileSizeLimitBytes: null,
                        shared: true,
                        retainedFileCountLimit: null)
                    .CreateLogger();
                _fileLogger.Write(logEvent);
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                if (_disposed)
                    return;

                _disposed = true;
                _fileLogger?.Dispose();
                _fileLogger = null;
            }
        }
    }

    private sealed class LogTextFormatter : ITextFormatter
    {
        private readonly bool _useColor;

        public LogTextFormatter(bool useColor)
        {
            _useColor = useColor;
        }

        public void Format(LogEvent logEvent, TextWriter output)
        {
            string color = ShouldUseColor() ? GetAnsiColor(logEvent.Level) : "";
            if (color.Length > 0)
                output.Write(color);

            output.Write($"[{logEvent.Timestamp.LocalDateTime:yyyy/MM/dd HH:mm:ss}] ");
            output.Write(GetStringProperty(logEvent, RoleDisplayProperty));
            output.Write('[');
            output.Write(GetStringProperty(logEvent, LevelNameProperty) ?? GetSerilogLevelName(logEvent.Level));
            output.Write("] | ");
            output.Write(logEvent.RenderMessage());
            output.WriteLine();

            if (logEvent.Exception != null)
                output.WriteLine(logEvent.Exception);

            if (color.Length > 0)
                output.Write("\u001b[0m");
        }

        private bool ShouldUseColor()
            => _useColor &&
               !Console.IsOutputRedirected &&
               !Console.IsErrorRedirected &&
               string.IsNullOrEmpty(Environment.GetEnvironmentVariable("NO_COLOR"));

        private static string GetAnsiColor(LogEventLevel level) => level switch
        {
            LogEventLevel.Verbose => "\u001b[90m",
            LogEventLevel.Debug => "\u001b[36m",
            LogEventLevel.Information => "\u001b[33m",
            LogEventLevel.Warning => "\u001b[35m",
            LogEventLevel.Error => "\u001b[31m",
            LogEventLevel.Fatal => "\u001b[91m",
            _ => ""
        };

        private static string GetSerilogLevelName(LogEventLevel level) => level switch
        {
            LogEventLevel.Verbose => "TRACE",
            LogEventLevel.Debug => "DEBUG",
            LogEventLevel.Information => "INFO",
            LogEventLevel.Warning => "WARN",
            LogEventLevel.Error => "ERROR",
            LogEventLevel.Fatal => "CRITICAL",
            _ => "INFO"
        };
    }
}
