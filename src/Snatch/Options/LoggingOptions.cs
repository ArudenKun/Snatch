using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using Serilog.Events;
using Snatch.Utilities;

namespace Snatch.Options;

public sealed partial class LoggingOptions : ObservableObject
{
    public const string Template =
        "[{Timestamp:yyyy-MM-dd HH:mm:ss}][{Level:u3}][{SourceContext}] {Message:lj}{NewLine}{Exception}";

    [ObservableProperty]
    public partial long Size { get; set; }

    [ObservableProperty]
    [JsonConverter(typeof(JsonStringEnumConverter<LogEventLevel>))]
    public partial LogEventLevel LogEventLevel { get; set; } =
        AppHelper.IsDebug ? LogEventLevel.Debug : LogEventLevel.Information;
}
