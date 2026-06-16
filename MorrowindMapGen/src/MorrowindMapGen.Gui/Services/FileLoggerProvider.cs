using Microsoft.Extensions.Logging;

namespace MorrowindMapGen.Gui.Services;

/// <summary>
/// A simple file logger provider that writes log messages to a file.
/// </summary>
public sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly string _filePath;
    private readonly LogLevel _minLevel;
    private readonly object _lock = new();
    private readonly StreamWriter _writer;

    public FileLoggerProvider(string filePath, LogLevel minLevel = LogLevel.Information)
    {
        _filePath = filePath;
        _minLevel = minLevel;

        // Create or overwrite the log file
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _writer = new StreamWriter(filePath, append: false) { AutoFlush = true };
        _writer.WriteLine($"=== Log started at {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new FileLogger(categoryName, _minLevel, _lock, _writer);
    }

    public void Dispose()
    {
        _writer.WriteLine($"=== Log ended at {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");
        _writer.Dispose();
    }

    private sealed class FileLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly LogLevel _minLevel;
        private readonly object _lock;
        private readonly StreamWriter _writer;

        public FileLogger(string categoryName, LogLevel minLevel, object lockObj, StreamWriter writer)
        {
            _categoryName = categoryName;
            _minLevel = minLevel;
            _lock = lockObj;
            _writer = writer;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= _minLevel;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            var message = formatter(state, exception);
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            var level = logLevel switch
            {
                LogLevel.Trace => "TRC",
                LogLevel.Debug => "DBG",
                LogLevel.Information => "INF",
                LogLevel.Warning => "WRN",
                LogLevel.Error => "ERR",
                LogLevel.Critical => "CRT",
                _ => "???"
            };

            // Extract short category name (last part after dot)
            var shortCategory = _categoryName;
            var lastDot = _categoryName.LastIndexOf('.');
            if (lastDot >= 0 && lastDot < _categoryName.Length - 1)
            {
                shortCategory = _categoryName[(lastDot + 1)..];
            }

            var logLine = $"[{timestamp}] [{level}] [{shortCategory}] {message}";

            lock (_lock)
            {
                _writer.WriteLine(logLine);

                if (exception != null)
                {
                    _writer.WriteLine($"  Exception: {exception}");
                }
            }
        }
    }
}
