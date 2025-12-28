using System.Text.Json.Serialization;
using Snatch.Core;
using Snatch.Dependency;
using Snatch.Utilities;

namespace Snatch.Services;

public sealed partial class DataService : JsonFileBase, ISingletonDependency
{
    public DataService()
        : base(AppHelper.DataPath, DataServiceSerializerContext.Default.Options) { }

    [JsonSerializable(typeof(DataService))]
    [JsonSourceGenerationOptions(WriteIndented = false)]
    private sealed partial class DataServiceSerializerContext : JsonSerializerContext;
}
