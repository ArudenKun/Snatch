using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ServiceScan.SourceGenerator;
using Snatch.Core.Utilities.Extensions;
using Snatch.Dependency;
using Snatch.Options;
using Snatch.Utilities;
using ZLinq;

namespace Snatch.Services;

public sealed partial class SettingsService : ISingletonDependency
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SettingsService> _logger;
    private readonly Dictionary<object, PropertyInfo[]> _rootMap = [];
    private readonly Dictionary<OptionAttribute, object?> _sectionMap = new();

    public SettingsService(IServiceProvider serviceProvider, ILogger<SettingsService> logger)
    {
        FilePath = AppHelper.SettingsPath;

        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public string FilePath { get; }

    public void Save()
    {
        File.WriteAllBytes(FilePath, SaveCore());
    }

    public async Task SaveAsync()
    {
        await File.WriteAllBytesAsync(FilePath, SaveCore());
    }

    private byte[] SaveCore()
    {
        // 1. Clear previous state to prevent duplicate key errors on re-save
        _rootMap.Clear();
        _sectionMap.Clear();

        // 2. Populate dictionaries via the Source Generator handler
        PopulateOptions();

        // 3. Create a master dictionary to merge Root properties and Section objects
        var finalData = new Dictionary<string, object?>();

        // 4. FIRST: Flatten root options into the master dictionary
        // This ensures they appear at the top of the JSON file.
        foreach (var (instance, properties) in _rootMap)
        {
            foreach (var prop in properties)
            {
                var value = prop.GetValue(instance);
                finalData[prop.Name] = value;
            }
        }

        // 5. SECOND: Add the sections
        foreach (
            var (attribute, sectionValue) in _sectionMap
                .AsValueEnumerable()
                .OrderBy(x => x.Key.Order)
        )
        {
            finalData[attribute.Section] = sectionValue;
        }

        // 6. Ensure directory exists
        var dirPath = Path.GetDirectoryName(FilePath);
        if (!string.IsNullOrWhiteSpace(dirPath))
        {
            Directory.CreateDirectory(dirPath);
        }

        // 7. Serialize and write safely
#pragma warning disable IL2026
#pragma warning disable IL3050
        var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(
#pragma warning restore IL3050
#pragma warning restore IL2026
            finalData,
            SettingsServiceSerializerContext.Default.Options
        );
        _logger.LogInformation("Saving settings file {SettingsFilePath}", FilePath);
        return jsonBytes;
    }

    [GenerateServiceRegistrations(
        AttributeFilter = typeof(OptionAttribute),
        CustomHandler = nameof(PopulateOptionsHandler)
    )]
    private partial void PopulateOptions();

    private void PopulateOptionsHandler<
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicProperties
                | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor
        )]
            T
    >()
        where T : class
    {
        var option = _serviceProvider.GetService<IOptions<T>>()?.Value;
        if (option is null)
            return;

        var type = typeof(T);
        var attribute = type.GetCustomAttribute<OptionAttribute>()!;
        var section = attribute.Section;

        if (section.IsNullOrWhiteSpace())
        {
            var propertyInfos = type.GetProperties();
            _rootMap.Add(option, propertyInfos);
        }
        else
        {
            _sectionMap.Add(attribute, option);
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
