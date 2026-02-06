using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Snatch.Dependency;
using Snatch.Models;
using Snatch.Options;
using Snatch.Utilities;

namespace Snatch.Services;

public partial class SettingsService : IDisposable, ISingletonDependency
{
    private readonly ILogger<SettingsService> _logger;
    private readonly ConcurrentDictionary<Type, Lazy<object>> _settings = new();
    private readonly JsonSerializerOptions _serializerOptions;
    private int _isDisposed;

    /// <summary>Initializes a new instance of the SettingsService.</summary>
    public SettingsService(ILogger<SettingsService> logger)
    {
        _logger = logger;
        FileName = AppHelper.SettingsPath;

        _serializerOptions = SettingsServiceSerializerContext.Default.Options;
    }

    public string FileName { get; }

    public event EventHandler<SettingsErrorEventArgs>? ErrorOccurred;

    public T Get<T>()
        where T : class, new() =>
        (T)_settings.GetOrAdd(typeof(T), key => new Lazy<object>(() => Load(key))).Value;

    public void Save()
    {
        var settingsList = _settings.ToArray(); // Snapshot of current loaded settings
        if (settingsList.Length < 1)
            return;

        try
        {
            JsonObject? rootNode = null;

            // 1. Try to load existing file to preserve settings not currently in memory
            if (File.Exists(FileName))
            {
                try
                {
                    using var stream = new FileStream(
                        FileName,
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.Read
                    );
                    // Parse into a mutable JsonNode
                    rootNode = JsonNode.Parse(stream)?.AsObject();
                }
                catch (Exception ex)
                {
                    // _logger.LogError(ex, "Error reading settings file");
                    OnErrorOccurred(
                        new SettingsErrorEventArgs(ex, SettingsServiceAction.Save, FileName)
                    );
                }
            }
            else
            {
                Directory.CreateDirectory(
                    Path.GetDirectoryName(FileName)
                        ?? throw new DirectoryNotFoundException("Directory not found: " + FileName)
                );
            }

            // 2. Initialize if null (new file or corrupted read)
            rootNode ??= new JsonObject();

            // 3. Update the JsonObject with current in-memory settings
            try
            {
                UpdateJsonNode(rootNode, settingsList);
            }
            catch (Exception ex)
            {
                OnErrorOccurred(
                    new SettingsErrorEventArgs(ex, SettingsServiceAction.Save, FileName)
                );
                // If update failed, reset to empty to ensure we at least save the current state
                rootNode = new JsonObject();
                UpdateJsonNode(rootNode, settingsList);
            }

            // 4. Write back to disk
            using (
                var stream = new FileStream(
                    FileName,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.Write
                )
            )
            {
                using var writer = new Utf8JsonWriter(
                    stream,
                    new JsonWriterOptions { Indented = true }
                );
                rootNode.WriteTo(writer, _serializerOptions);
            }
        }
        catch (Exception ex)
        {
            OnErrorOccurred(new SettingsErrorEventArgs(ex, SettingsServiceAction.Save, FileName));
        }
    }

    private void UpdateJsonNode(JsonObject root, KeyValuePair<Type, Lazy<object>>[] settingsList)
    {
        foreach (var kvp in settingsList)
        {
            // Ensure we only save values that have actually been instantiated
            if (kvp.Value.IsValueCreated)
            {
                var typeKey = GetTypeKey(kvp.Key);
                var settingObj = kvp.Value.Value;

                // Serialize the specific setting object to a node
                var settingNode = JsonSerializer.SerializeToNode(
                    settingObj,
                    kvp.Key,
                    _serializerOptions
                );

                // Update or Add to the root object
                root[typeKey] = settingNode;
            }
        }
    }

    /// <summary>Calls save to ensure that the latest changes are persisted.</summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected void Dispose(bool disposing)
    {
        if (Interlocked.CompareExchange(ref _isDisposed, 1, 0) != 0)
            return;
        OnDispose(disposing);
        if (disposing)
            Save();
    }

    protected virtual void OnDispose(bool disposing) { }

    private void OnErrorOccurred(SettingsErrorEventArgs e)
    {
        _logger.LogError(e.Error, "An error occurred on [{Action}]", e.Action);
        ErrorOccurred?.Invoke(this, e);
    }

    private object Load(Type type)
    {
        object? settingObject = null;
        try
        {
            settingObject = LoadCore(type);
        }
        catch (Exception ex)
        {
            OnErrorOccurred(new SettingsErrorEventArgs(ex, SettingsServiceAction.Open, FileName));
        }

        return settingObject ?? Activator.CreateInstance(type)!;
    }

    private object? LoadCore(Type type)
    {
        if (!File.Exists(FileName))
            return null;

        try
        {
            using var stream = new FileStream(
                FileName,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read
            );

            // Parse the whole file structure
            var rootNode = JsonNode.Parse(stream);
            if (rootNode is JsonObject rootObj)
            {
                var typeKey = GetTypeKey(type);

                // Find the key corresponding to this type
                if (rootObj.TryGetPropertyValue(typeKey, out var section) && section != null)
                {
                    return section.Deserialize(type, _serializerOptions);
                }
            }
        }
        catch (JsonException)
        {
            // JSON might be corrupted; return null so a default instance is created.
            // The Save() method will handle overwriting the bad file later.
        }

        return null;
    }

    /// <summary>
    /// Generates a stable key for the dictionary based on the Type.
    /// Using FullName handles nested types correctly.
    /// </summary>
    private static string GetTypeKey(Type type)
    {
        string? name = null;
        while (type.IsNested)
        {
            name = name == null ? type.Name : type.Name + "." + name;
            type = type.DeclaringType!;
        }
        name = name == null ? type.Name : type.Name + "." + name;
        return name;
    }

    [JsonSerializable(typeof(AppearanceOptions))]
    [JsonSerializable(typeof(GeneralOptions))]
    [JsonSerializable(typeof(LoggingOptions))]
    [JsonSerializable(typeof(YoutubeOptions))]
    [JsonSourceGenerationOptions(WriteIndented = true, UseStringEnumConverter = true)]
    private sealed partial class SettingsServiceSerializerContext : JsonSerializerContext;
}
