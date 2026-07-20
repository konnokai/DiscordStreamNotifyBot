public static class Log
{
    public enum LogLevel { Trace, Debug, Info, Warn, Error, Critical }

    /// <summary>多程序部署的角色標籤（如 <c>scraper</c> / <c>notifier:0</c> / <c>coordinator</c>），跨程序追蹤用（計畫 §10）。空字串則不加。</summary>
    public static string RolePrefix { get; set; } = "";

    /// <summary>從完整 log 行擷取 timestamp、role、level、message 的 .NET regex。</summary>
    public const string LogLinePattern = @"^\[(?<timestamp>\d{4}/\d{2}/\d{2} \d{2}:\d{2}:\d{2})\] (?:\[(?<role>[^\]]+)\] )?\[(?<level>TRACE|DEBUG|INFO|WARN|ERROR|CRITICAL)\] \| (?<message>.*)$";

    /// <summary>只偵測標準 log level 的 regex，供不解析完整行的收集器使用。</summary>
    public const string LogLevelPattern = @"\[(?<level>TRACE|DEBUG|INFO|WARN|ERROR|CRITICAL)\]";

    /// <summary>Docker／Container 環境只輸出 stdout/stderr，不在暫存檔案系統寫入 Log。</summary>
    public static bool IsRunningInContainer { get; } =
        IsTrueEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") ||
        IsTrueEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINERS");

    enum LogFileType { General, Stream, Error }
    static string logPath = DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") + ".log";
    static string errorLogPath = DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") + "_err.log";
    static string streamLogPath = DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") + "_stream.log";
    private static readonly object writeLockObj = new();
    private static readonly object logLockObj = new();
    private static readonly object lokiLockObj = new();
    private static LokiLogSink lokiSink;
    private static int processExitHandlerRegistered;

    private static void WriteLogToFile(LogFileType fileType, string line)
    {
        if (Debugger.IsAttached || IsRunningInContainer)
            return;

        lock (writeLockObj)
        {
            line += "\r\n";

            switch (fileType)
            {
                case LogFileType.Error:
                    File.AppendAllText(errorLogPath, line);
                    break;
                case LogFileType.Stream:
                    File.AppendAllText(streamLogPath, line);
                    break;
            }

            File.AppendAllText(logPath, line);
        }
    }

    private static bool IsTrueEnvironmentVariable(string name)
    {
        string value = Environment.GetEnvironmentVariable(name);
        return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) || value == "1";
    }

    public static void New(string text, bool newLine = true)
    {
        WriteEntry(text, ConsoleColor.Green, newLine, false, LogLevel.Info, LogFileType.Stream);
    }

    public static void Debug(string text, bool newLine = true)
    {
        if (!Debugger.IsAttached)
            return;

        WriteEntry(text, ConsoleColor.Cyan, newLine, false, LogLevel.Debug);
    }

    public static void Info(string text, bool newLine = true)
    {
        WriteEntry(text, ConsoleColor.DarkYellow, newLine, false, LogLevel.Info, LogFileType.General);
    }

    public static void Warn(string text, bool newLine = true)
    {
        WriteEntry(text, ConsoleColor.DarkMagenta, newLine, false, LogLevel.Warn, LogFileType.General);
    }

    public static void Error(string text, bool newLine = true, bool writeLog = true)
    {
        WriteEntry(text, ConsoleColor.DarkRed, newLine, false, LogLevel.Error,
            writeLog ? LogFileType.Error : null);
    }

    public static void Error(Exception ex, string text, bool newLine = true, bool writeLog = true)
    {
        LogFileType? fileType = writeLog ? LogFileType.Error : null;
        WriteEntry(text, ConsoleColor.DarkRed, newLine, true, LogLevel.Error, fileType);
        WriteEntry(ex.Demystify().ToString(), ConsoleColor.DarkRed, true, true, LogLevel.Error, fileType);
    }

    public static void FormatColorWrite(string text, ConsoleColor consoleColor = ConsoleColor.Gray,
        bool newLine = true, bool isError = false, LogLevel level = LogLevel.Info)
    {
        WriteEntry(text, consoleColor, newLine, isError, level);
    }

    /// <summary>啟用 Loki 主動推送。未設定或 URL 無效時保留既有 console/file 行為。</summary>
    public static void ConfigureLoki(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return;

        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri endpoint) ||
            (endpoint.Scheme != Uri.UriSchemeHttp && endpoint.Scheme != Uri.UriSchemeHttps))
        {
            WriteLokiDiagnostic($"LOKI_URL 無效，略過 Loki 主動推送：{url}");
            return;
        }

        lock (lokiLockObj)
        {
            if (lokiSink != null)
                return;

            lokiSink = new LokiLogSink(endpoint, WriteLokiDiagnostic);
            if (Interlocked.Exchange(ref processExitHandlerRegistered, 1) == 0)
            {
                AppDomain.CurrentDomain.ProcessExit += (_, _) =>
                {
                    try { ShutdownAsync(TimeSpan.FromSeconds(2)).GetAwaiter().GetResult(); }
                    catch { }
                };
            }
        }

        Info($"Loki 主動推送已啟用：{endpoint}");
    }

    /// <summary>等待 Loki 佇列送出；逾時後直接結束，不阻塞程序關閉。</summary>
    public static async Task ShutdownAsync(TimeSpan timeout)
    {
        LokiLogSink sink;
        lock (lokiLockObj)
        {
            sink = lokiSink;
            lokiSink = null;
        }

        if (sink != null)
            await sink.StopAsync(timeout);
    }

    private static void WriteEntry(string text, ConsoleColor consoleColor, bool newLine, bool isError,
        LogLevel level, LogFileType? fileType = null, bool writeConsole = true)
    {
        string role;
        string line;
        lock (logLockObj)
        {
            role = RolePrefix;
            line = FormatLine(level, text, role);

            if (writeConsole)
                WriteConsole(line, consoleColor, newLine, isError);

            if (fileType.HasValue)
                WriteLogToFile(fileType.Value, line);
        }

        LokiLogSink sink;
        lock (lokiLockObj)
            sink = lokiSink;
        sink?.TryEnqueue(level, role, line);
    }

    private static void WriteConsole(string line, ConsoleColor consoleColor, bool newLine, bool isError)
    {
        Console.ForegroundColor = consoleColor;

        if (isError)
        {
            if (newLine)
            {
                Console.Error.WriteLine(line);
            }
            else
            {
                Console.Error.Write(line);
            }
        }
        else
        {
            if (newLine)
            {
                Console.WriteLine(line);
            }
            else
            {
                Console.Write(line);
            }
        }

        Console.ForegroundColor = ConsoleColor.Gray;
    }

    private static void WriteLokiDiagnostic(string text)
    {
        string line = FormatLine(LogLevel.Warn, text, RolePrefix);
        Console.Error.WriteLine(line);
        WriteLogToFile(LogFileType.General, line);
    }

    private static string FormatLine(LogLevel level, string text, string role)
    {
        string tag = string.IsNullOrEmpty(role) ? "" : $"[{role}] ";
        return $"[{DateTime.Now:yyyy/MM/dd HH:mm:ss}] {tag}[{level.ToString().ToUpperInvariant()}] | {text}";
    }

    public static Task LogMsg(LogMessage message)
    {
        ConsoleColor consoleColor = ConsoleColor.DarkCyan;
        LogLevel level = LogLevel.Info;

        switch (message.Severity)
        {
            case LogSeverity.Critical:
                consoleColor = ConsoleColor.DarkRed;
                level = LogLevel.Critical;
                break;
            case LogSeverity.Error:
                consoleColor = ConsoleColor.DarkRed;
                level = LogLevel.Error;
                break;
            case LogSeverity.Warning:
                consoleColor = ConsoleColor.DarkMagenta;
                level = LogLevel.Warn;
                break;
            case LogSeverity.Debug:
                consoleColor = ConsoleColor.Green;
                level = LogLevel.Debug;
                break;
            case LogSeverity.Verbose:
                level = LogLevel.Trace;
                break;
        }

#if DEBUG || DEBUG_DONTREGISTERCOMMAND
        if (!string.IsNullOrEmpty(message.Message))
            WriteEntry(message.Message, consoleColor, true, false, level);
#else
        if (IsRunningInContainer)
            WriteEntry(message.Message, consoleColor, true, false, level);
        else
            WriteEntry(message.Message, consoleColor, true, false, level, LogFileType.General, false);
#endif

        if (message.Exception != null &&
            message.Message != null &&
            !message.Message.Contains("TYPING_START") &&
            (message.Exception is not GatewayReconnectException &&
            message.Exception is not TaskCanceledException &&
            message.Exception is not JsonSerializationException &&
            message.Exception is not NullReferenceException))
        {
            consoleColor = ConsoleColor.DarkRed;
#if RELEASE
            WriteEntry(message.Message, consoleColor, true, false, level);
#endif
            WriteEntry(message.Exception.GetType().FullName, consoleColor, true, false, LogLevel.Error);
            WriteEntry(message.Exception.Message, consoleColor, true, false, LogLevel.Error);
            WriteEntry(message.Exception.StackTrace, consoleColor, true, false, LogLevel.Error);
        }

        return Task.CompletedTask;
    }
}
