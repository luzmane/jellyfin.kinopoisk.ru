namespace Jellyfin.Plugin.KinopoiskRu.Tests.Utils;

public class JellyfinLogger<T> : ILogger<T>
{
    private readonly NLog.Logger _logger;

    public JellyfinLogger()
    {
        _logger = NLog.LogManager.GetLogger(typeof(T).Name);
    }

    public IDisposable BeginScope<TState>(TState state)
    {
        return null!;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return _logger.IsEnabled(logLevel switch
        {
            LogLevel.Trace => NLog.LogLevel.Trace,
            LogLevel.Debug => NLog.LogLevel.Debug,
            LogLevel.Information => NLog.LogLevel.Info,
            LogLevel.Warning => NLog.LogLevel.Warn,
            LogLevel.Error => NLog.LogLevel.Error,
            LogLevel.Critical => NLog.LogLevel.Fatal,
            LogLevel.None => NLog.LogLevel.Off,
            _ => NLog.LogLevel.Off
        });
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (formatter == null)
        {
            throw new ArgumentNullException(nameof(formatter));
        }
        _logger.Info(exception, formatter(state, exception));
    }

}
