using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Snatch.Core;
using Snatch.Dependency;
using Snatch.Utilities;

namespace Snatch.Services;

public sealed partial class DataService
    : JsonFileBase,
        ISingletonDependency,
        IInitializer,
        IFinalizer
{
    private readonly ILogger<DataService> _logger;

    public DataService(ILogger<DataService> logger)
        : base(AppHelper.DataPath, DataServiceSerializerContext.Default.Options)
    {
        _logger = logger;
    }

    public string Test { get; set; } = "Yeet";

    public void OnCreate()
    {
        _logger.LogInformation("Loaded DataService");
        Load();
    }

    public void OnDestroy()
    {
        _logger.LogInformation("Saved DataService");
        Save();
    }

    [JsonSerializable(typeof(DataService))]
    [JsonSourceGenerationOptions(WriteIndented = false)]
    private sealed partial class DataServiceSerializerContext : JsonSerializerContext;
}
