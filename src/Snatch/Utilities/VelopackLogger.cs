using Serilog.Events;
using Snatch.Core;
using Velopack;
using Velopack.Logging;

namespace Snatch.Utilities;

public class VelopackLogger : SingletonBase<VelopackLogger>, IVelopackLogger
{
    public void Log(VelopackLogLevel logLevel, string? message, Exception? exception)
    {
        Serilog
            .Log.Logger.ForContext<VelopackApp>()
            .Write(MapToSerilogLogEventLevel(logLevel), exception, "{Message}", message);
    }

    private static LogEventLevel MapToSerilogLogEventLevel(VelopackLogLevel logLevel) =>
        logLevel switch
        {
            VelopackLogLevel.Trace => LogEventLevel.Verbose,
            VelopackLogLevel.Debug => LogEventLevel.Debug,
            VelopackLogLevel.Information => LogEventLevel.Information,
            VelopackLogLevel.Warning => LogEventLevel.Warning,
            VelopackLogLevel.Error => LogEventLevel.Error,
            VelopackLogLevel.Critical => LogEventLevel.Fatal,
            _ => throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null),
        };
}
