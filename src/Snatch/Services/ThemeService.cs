using AutoInterfaceAttributes;
using Avalonia.Collections;
using Avalonia.Styling;
using JetBrains.Annotations;
using R3;
using Snatch.Models;
using SukiUI;
using SukiUI.Enums;
using SukiUI.Models;
using Volo.Abp.DependencyInjection;
using ZLinq;
using Color = Avalonia.Media.Color;

namespace Snatch.Services;

[AutoInterface(Inheritance = [typeof(IDisposable)])]
[UsedImplicitly]
public sealed class ThemeService : IThemeService, ISingletonDependency
{
    private readonly SettingsService _settingsService;
    private readonly IDisposable _subscriptions;

    public ThemeService(SettingsService settingsService)
    {
        _settingsService = settingsService;

        _subscriptions = Disposable.Combine(
            _settingsService
                .Appearance.ObservePropertyChanged(x => x.Theme, false)
                .ObserveOnUIThreadDispatcher()
                .Subscribe(ChangeTheme),
            _settingsService
                .Appearance.ObservePropertyChanged(x => x.ThemeColor)
                .ObserveOnUIThreadDispatcher()
                .Subscribe(colorThemeDisplayName =>
                    ChangeColorTheme(ResolveColorTheme(colorThemeDisplayName))
                )
        );
    }

    private SukiTheme SukiTheme => field ??= SukiTheme.GetInstance();

    public Theme CurrentTheme => _settingsService.Appearance.Theme;

    public SukiColorTheme CurrentColorTheme =>
        ResolveColorTheme(_settingsService.Appearance.ThemeColor);

    public IAvaloniaReadOnlyList<SukiColorTheme> ColorThemes => SukiTheme.ColorThemes;

    public void Initialize()
    {
        SukiTheme.AddColorThemes([
            new SukiColorTheme("Pink", new Color(255, 255, 20, 147), new Color(255, 255, 192, 203)),
            new SukiColorTheme("White", new Color(255, 255, 255, 255), new Color(255, 0, 0, 0)),
            new SukiColorTheme("Black", new Color(255, 0, 0, 0), new Color(255, 255, 255, 255)),
        ]);
        ChangeTheme(_settingsService.Appearance.Theme);
        ChangeColorTheme(ResolveColorTheme(_settingsService.Appearance.ThemeColor));
    }

    public void ChangeTheme(Theme theme)
    {
        _settingsService.Appearance.Theme = theme;
        var variant = theme switch
        {
            Theme.System => ThemeVariant.Default,
            Theme.Light => ThemeVariant.Light,
            Theme.Dark => ThemeVariant.Dark,
            _ => throw new ArgumentOutOfRangeException(nameof(theme), theme, null),
        };
        SukiTheme.ChangeBaseTheme(variant);
    }

    public void ChangeColorTheme(SukiColorTheme colorTheme)
    {
        _settingsService.Appearance.ThemeColor = colorTheme.DisplayName;
        SukiTheme.ChangeColorTheme(colorTheme);
    }

    private SukiColorTheme ResolveColorTheme(string? displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            return SukiTheme.DefaultColorThemes[SukiColor.Blue];

        return SukiTheme
                .ColorThemes.AsValueEnumerable()
                .FirstOrDefault(theme =>
                    theme.DisplayName.Equals(displayName, StringComparison.OrdinalIgnoreCase)
                )
            ?? SukiTheme.DefaultColorThemes[SukiColor.Blue];
    }

    public void Dispose() => _subscriptions.Dispose();
}
