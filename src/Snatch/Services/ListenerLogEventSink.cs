using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Riok.Mapperly.Abstractions;
using Serilog.Core;
using Serilog.Events;
using Snatch.Models;
using Volo.Abp.DependencyInjection;

namespace Snatch.Services;

public sealed partial class ListenerLogEventSink : ILogEventSink, ISingletonDependency
{
    private readonly IServiceProvider _serviceProvider;
    private bool _initialized;

    public ListenerLogEventSink(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void Initialize()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
    }

    public void Emit(LogEvent logEvent)
    {
        if (!_initialized)
        {
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var messenger = scope.ServiceProvider.GetRequiredService<IMessenger>();
        messenger.Send(LogEntryMapper.Map(logEvent));
    }

    [Mapper(AutoUserMappings = false, RequiredMappingStrategy = RequiredMappingStrategy.None)]
    private static partial class LogEntryMapper
    {
        [MapProperty(nameof(LogEvent.Level), nameof(LogEntry.LogLevel), Use = nameof(MapLogEvent))]
        [MapPropertyFromSource(nameof(LogEntry.EventId), Use = nameof(MapEventId))]
        [MapPropertyFromSource(nameof(LogEntry.State), Use = nameof(MapState))]
        public static partial LogEntry Map(LogEvent source);

        [MapProperty(nameof(LogEvent.Level), nameof(LogEntry.LogLevel), Use = nameof(MapLogEvent))]
        [MapPropertyFromSource(nameof(LogEntry.EventId), Use = nameof(MapEventId))]
        [MapPropertyFromSource(nameof(LogEntry.State), Use = nameof(MapState))]
        public static partial void Map(LogEvent source, LogEntry destination);

        [UserMapping]
        private static object MapState(LogEvent logEvent)
        {
            return logEvent.RenderMessage();
        }

        [UserMapping]
        private static LogLevel MapLogEvent(LogEventLevel eventLevel) =>
            eventLevel switch
            {
                LogEventLevel.Debug => LogLevel.Debug,
                LogEventLevel.Error => LogLevel.Error,
                LogEventLevel.Fatal => LogLevel.Critical,
                LogEventLevel.Verbose => LogLevel.Trace,
                LogEventLevel.Warning => LogLevel.Warning,
                _ => LogLevel.Information,
            };

        [UserMapping]
        private static EventId MapEventId(LogEvent logEvent)
        {
            // Serilog.Extensions.Logging stores EventId as a StructureValue property named "EventId"
            if (
                logEvent.Properties.TryGetValue("EventId", out var propertyValue)
                && propertyValue is StructureValue structure
            )
            {
                var idProperty = structure.Properties.FirstOrDefault(p => p.Name == "Id");
                var nameProperty = structure.Properties.FirstOrDefault(p => p.Name == "Name");

                int id = 0;
                if (idProperty?.Value is ScalarValue { Value: int idVal })
                {
                    id = idVal;
                }

                string? name = null;
                if (nameProperty?.Value is ScalarValue { Value: string nameVal })
                {
                    name = nameVal;
                }

                return new EventId(id, name);
            }

            // Fallback: If no EventId structure exists, return default
            return new EventId(0);
        }
    }
}
