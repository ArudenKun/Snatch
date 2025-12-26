using Microsoft.Extensions.Logging;

namespace Snatch.Models;

public class LogEntry
{
    public DateTimeOffset Timestamp { get; set; }

    public LogLevel LogLevel { get; set; }

    public EventId EventId { get; set; }

    public object? State { get; set; }

    public string? Exception { get; set; }
}
