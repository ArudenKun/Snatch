using Microsoft.Extensions.Logging;

namespace Snatch.Models.Messages;

public class LogMessage
{
    public DateTimeOffset Timestamp { get; set; }

    public LogLevel LogLevel { get; set; }

    public EventId EventId { get; set; }

    public object? State { get; set; }

    public string? Exception { get; set; }
}
