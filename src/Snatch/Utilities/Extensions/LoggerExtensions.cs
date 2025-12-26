using Microsoft.Extensions.Logging;

namespace Snatch.Utilities.Extensions;

public static class LoggerExtensions
{
    public static void Emit(
        this ILogger? logger,
        EventId eventId,
        LogLevel logLevel,
        string message,
        Exception? exception = null,
        params object?[] args
    )
    {
        if (logger is null)
            return;

        switch (logLevel)
        {
            case LogLevel.Trace:
                logger.LogTrace(eventId, message, args);
                break;

            case LogLevel.Debug:
                logger.LogDebug(eventId, message, args);
                break;

            case LogLevel.Information:
                logger.LogInformation(eventId, message, args);
                break;

            case LogLevel.Warning:
                logger.LogWarning(eventId, exception, message, args);
                break;

            case LogLevel.Error:
                logger.LogError(eventId, exception, message, args);
                break;

            case LogLevel.Critical:
                logger.LogCritical(eventId, exception, message, args);
                break;
        }
    }
}
