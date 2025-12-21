using System.Text.Json.Serialization;
using Avalonia.Controls;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using Humanizer;
using Snatch.Models;
using SukiUI.Enums;

namespace Snatch.Options;

public sealed partial class AppearanceOptions : ObservableObject
{
    [ObservableProperty]
    [JsonConverter(typeof(JsonStringEnumConverter<Theme>))]
    public partial Theme Theme { get; set; } = Theme.System;

    [JsonIgnore]
    public ThemeVariant ThemeVariant =>
        Theme switch
        {
            Theme.Light => ThemeVariant.Light,
            Theme.Dark => ThemeVariant.Dark,
            _ => ThemeVariant.Default,
        };

    [ObservableProperty]
    public partial string ThemeColor { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool BackgroundAnimations { get; set; } = true;

    [ObservableProperty]
    public partial bool BackgroundTransitions { get; set; } = true;

    [ObservableProperty]
    [JsonConverter(typeof(JsonStringEnumConverter<SukiBackgroundStyle>))]
    public partial SukiBackgroundStyle BackgroundStyle { get; set; } =
        SukiBackgroundStyle.GradientSoft;

    [ObservableProperty]
    [JsonConverter(typeof(JsonStringEnumConverter<WindowState>))]
    public partial WindowState LastWindowState { get; set; } = WindowState.Normal;

    [ObservableProperty]
    public partial TimeSpan ToastDuration { get; set; } = 5.Seconds();
}
