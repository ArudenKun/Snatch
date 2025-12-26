using System.Text.Json.Serialization;
using Snatch.Core;
using Snatch.Utilities;
using Volo.Abp.DependencyInjection;

namespace Snatch.Services;

public sealed partial class DataService : JsonFileBase, ISingletonDependency
{
    public DataService()
        : base(AppHelper.DataPath, DataServiceSerializerContext.Default.Options) { }

    [JsonSerializable(typeof(DataService))]
    [JsonSourceGenerationOptions(WriteIndented = false)]
    private sealed partial class DataServiceSerializerContext : JsonSerializerContext;
}
