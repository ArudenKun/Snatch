using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using Humanizer;
using ShadUI;

namespace Snatch.Options;

[Option("Appearance")]
public sealed partial class AppearanceOptions : ObservableObject
{
    [ObservableProperty]
    public partial ThemeMode Theme { get; set; } = ThemeMode.System;

    [ObservableProperty]
    public partial bool BackgroundAnimations { get; set; } = true;

    [ObservableProperty]
    public partial bool BackgroundTransitions { get; set; } = true;

    [ObservableProperty]
    public partial WindowState LastWindowState { get; set; } = WindowState.Normal;

    [ObservableProperty]
    public partial TimeSpan ToastDuration { get; set; } = 5.Seconds();
}
