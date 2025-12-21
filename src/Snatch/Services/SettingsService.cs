using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using Snatch.Core;
using Snatch.Options;
using Snatch.Utilities;
using Volo.Abp.DependencyInjection;

namespace Snatch.Services;

[ObservableObject]
public sealed partial class SettingsService : JsonFileBase, ISingletonDependency
{
    public event Action<bool>? Loaded;
    public event Action? Saved;

    public SettingsService()
        : base(AppHelper.SettingsPath, SettingServiceJsonSerializerContext.Default) { }

    [JsonIgnore]
    [ObservableProperty]
    public partial GeneralOptions General { get; set; } = new();

    [ObservableProperty]
    public partial YoutubeOptions Youtube { get; set; } = new();

    [ObservableProperty]
    public partial AppearanceOptions Appearance { get; set; } = new();

    [ObservableProperty]
    public partial LoggingOptions Logging { get; set; } = new();

    // Required for AutoInterface to see the methods
    // ReSharper disable RedundantOverriddenMember
    public override void Reset()
    {
        base.Reset();
    }

    public override void Save()
    {
        base.Save();
        Saved?.Invoke();
    }

    public override bool Load()
    {
        var result = base.Load();
        Loaded?.Invoke(result);
        return result;
    }

    // ReSharper restore RedundantOverriddenMember

    [JsonSerializable(typeof(SettingsService))]
    private sealed partial class SettingServiceJsonSerializerContext : JsonSerializerContext;
}
