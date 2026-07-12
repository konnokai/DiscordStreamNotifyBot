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

    private static string Tag => string.IsNullOrEmpty(RolePrefix) ? "" : $"[{RolePrefix}] ";

    enum LogFileType { General, Stream, Error }
    static string logPath = DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") + ".log";
    static string errorLogPath = DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") + "_err.log";
    static string streamLogPath = DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") + "_stream.log";
    private static readonly object writeLockObj = new();
    private static readonly object logLockObj = new();

    private static void WriteLogToFile(LogFileType fileType, LogLevel level, string text)
    {
        if (Debugger.IsAttached || IsRunningInContainer)
            return;

        lock (writeLockObj)
        {
            text = FormatLine(level, text) + "\r\n";

            switch (fileType)
            {
                case LogFileType.Error:
                    File.AppendAllText(errorLogPath, text);
                    break;
                case LogFileType.Stream:
                    File.AppendAllText(streamLogPath, text);
                    break;
            }

            File.AppendAllText(logPath, text);
        }
    }

    private static bool IsTrueEnvironmentVariable(string name)
    {
        string value = Environment.GetEnvironmentVariable(name);
        return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) || value == "1";
    }

    public static void New(string text, bool newLine = true)
    {
        lock (logLockObj)
        {
            FormatColorWrite(text, ConsoleColor.Green, newLine, level: LogLevel.Info);
            WriteLogToFile(LogFileType.Stream, LogLevel.Info, text);
        }
    }

    public static void Debug(string text, bool newLine = true)
    {
        if (!Debugger.IsAttached)
            return;

        lock (logLockObj)
        {
            FormatColorWrite(text, ConsoleColor.Cyan, newLine, level: LogLevel.Debug);
        }
    }

    public static void Info(string text, bool newLine = true)
    {
        lock (logLockObj)
        {
            FormatColorWrite(text, ConsoleColor.DarkYellow, newLine, level: LogLevel.Info);
            WriteLogToFile(LogFileType.General, LogLevel.Info, text);
        }
    }

    public static void Warn(string text, bool newLine = true)
    {
        lock (logLockObj)
        {
            FormatColorWrite(text, ConsoleColor.DarkMagenta, newLine, level: LogLevel.Warn);
            WriteLogToFile(LogFileType.General, LogLevel.Warn, text);
        }
    }

    public static void Error(string text, bool newLine = true, bool writeLog = true)
    {
        lock (logLockObj)
        {
            FormatColorWrite(text, ConsoleColor.DarkRed, newLine, level: LogLevel.Error);
            if (writeLog) WriteLogToFile(LogFileType.Error, LogLevel.Error, text);
        }
    }

    public static void Error(Exception ex, string text, bool newLine = true, bool writeLog = true)
    {
        lock (logLockObj)
        {
            FormatColorWrite(text, ConsoleColor.DarkRed, newLine, true, LogLevel.Error);
            FormatColorWrite(ex.Demystify().ToString(), ConsoleColor.DarkRed, true, true, LogLevel.Error);

            if (writeLog)
            {
                WriteLogToFile(LogFileType.Error, LogLevel.Error, $"{text}");
                WriteLogToFile(LogFileType.Error, LogLevel.Error, $"{ex}");
            }
        }
    }

    public static void FormatColorWrite(string text, ConsoleColor consoleColor = ConsoleColor.Gray,
        bool newLine = true, bool isError = false, LogLevel level = LogLevel.Info)
    {
        text = FormatLine(level, text);
        Console.ForegroundColor = consoleColor;

        if (isError)
        {
            if (newLine)
            {
                Console.Error.WriteLine(text);
            }
            else
            {
                Console.Error.Write(text);
            }
        }
        else
        {
            if (newLine)
            {
                Console.WriteLine(text);
            }
            else
            {
                Console.Write(text);
            }
        }

        Console.ForegroundColor = ConsoleColor.Gray;
    }

    private static string FormatLine(LogLevel level, string text)
        => $"[{DateTime.Now:yyyy/MM/dd HH:mm:ss}] {Tag}[{level.ToString().ToUpperInvariant()}] | {text}";

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
        if (!string.IsNullOrEmpty(message.Message)) FormatColorWrite(message.Message, consoleColor, level: level);
#else
        if (IsRunningInContainer)
            FormatColorWrite(message.Message, consoleColor, level: level);
        else
            WriteLogToFile(LogFileType.General, level, message.Message);
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
            FormatColorWrite(message.Message, consoleColor, level: level);
#endif
            FormatColorWrite(message.Exception.GetType().FullName, consoleColor, level: LogLevel.Error);
            FormatColorWrite(message.Exception.Message, consoleColor, level: LogLevel.Error);
            FormatColorWrite(message.Exception.StackTrace, consoleColor, level: LogLevel.Error);
        }

        return Task.CompletedTask;
    }
}
