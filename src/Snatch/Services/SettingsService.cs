using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ServiceScan.SourceGenerator;
using Snatch.Options;
using Snatch.Utilities;
using Volo.Abp.DependencyInjection;

namespace Snatch.Services;

public sealed partial class SettingsService : ISingletonDependency
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<object, PropertyInfo[]> _rootMap = [];
    private readonly Dictionary<string, object?> _sectionMap = new();

    public SettingsService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    private static string FilePath => AppHelper.SettingsPath;

    public void Save()
    {
        // 1. Clear previous state to prevent duplicate key errors on re-save
        _rootMap.Clear();
        _sectionMap.Clear();

        // 2. Populate dictionaries via the Source Generator handler
        GetOptions(_serviceProvider);

        // 3. Create a master dictionary to merge Root properties and Section objects
        var finalData = new Dictionary<string, object?>(_sectionMap);

        // 4. Flatten root options into the master dictionary
        foreach (var (instance, properties) in _rootMap)
        {
            foreach (var prop in properties)
            {
                var value = prop.GetValue(instance);
                finalData[prop.Name] = value;
            }
        }

        // 5. Ensure directory exists
        var dirPath = Path.GetDirectoryName(FilePath);
        if (!string.IsNullOrWhiteSpace(dirPath))
        {
            Directory.CreateDirectory(dirPath);
        }

        // 6. Serialize and write safely
        var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(
            finalData,
            SettingsServiceSerializerContext.Default.Options
        );
        File.WriteAllBytes(FilePath, jsonBytes);
    }

    [GenerateServiceRegistrations(
        AttributeFilter = typeof(OptionAttribute),
        CustomHandler = nameof(GetOptionsHandler)
    )]
    private partial void GetOptions(IServiceProvider serviceProvider);

    private void GetOptionsHandler<T>(IServiceProvider serviceProvider)
        where T : class
    {
        var option = serviceProvider.GetService<IOptions<T>>()?.Value;
        if (option is null)
            return;

        var type = typeof(T);
        var section = type.GetCustomAttribute<OptionAttribute>()?.Section;

        if (section.IsNullOrWhiteSpace())
        {
            var propertyInfos = type.GetProperties();
            _rootMap.Add(option, propertyInfos);
        }
        else
        {
            _sectionMap.Add(section, option);
        }
    }

    [JsonSerializable(typeof(Dictionary<string, object?>))]
    [JsonSerializable(typeof(AppearanceOptions))]
    [JsonSerializable(typeof(GeneralOptions))]
    [JsonSerializable(typeof(LoggingOptions))]
    [JsonSerializable(typeof(YoutubeOptions))]
    [JsonSourceGenerationOptions(WriteIndented = true, UseStringEnumConverter = true)]
    private sealed partial class SettingsServiceSerializerContext : JsonSerializerContext;
}
