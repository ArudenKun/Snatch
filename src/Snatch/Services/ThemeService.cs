using Avalonia.Collections;
using Avalonia.Media;
using Avalonia.Styling;
using Microsoft.Extensions.Options;
using R3;
using Snatch.Dependency;
using Snatch.Models;
using Snatch.Options;
using SukiUI;
using SukiUI.Enums;
using SukiUI.Models;
using ZLinq;

namespace Snatch.Services;

public sealed class ThemeService : IDisposable, ISingletonDependency
{
    private readonly IDisposable _subscriptions;
    private readonly AppearanceOptions _options;

    private bool _initialized;

    public ThemeService(IOptions<AppearanceOptions> options)
    {
        _options = options.Value;
        _subscriptions = Disposable.Combine(
            _options
                .ObservePropertyChanged(x => x.Theme, false)
                .ObserveOnUIThreadDispatcher()
                .Subscribe(ChangeTheme),
            _options
                .ObservePropertyChanged(x => x.ThemeColor)
                .ObserveOnUIThreadDispatcher()
                .Subscribe(colorThemeDisplayName =>
                    ChangeColorTheme(ResolveColorTheme(colorThemeDisplayName))
                )
        );
    }

    private static SukiTheme SukiTheme => field ??= SukiTheme.GetInstance();

    public Theme CurrentTheme => _options.Theme;

    public SukiColorTheme CurrentColorTheme => ResolveColorTheme(_options.ThemeColor);

    public IAvaloniaReadOnlyList<SukiColorTheme> ColorThemes => SukiTheme.ColorThemes;

    public void Initialize()
    {
        if (_initialized)
            return;

        SukiTheme.AddColorThemes([
            new SukiColorTheme("Pink", new Color(255, 255, 20, 147), new Color(255, 255, 192, 203)),
            new SukiColorTheme("White", new Color(255, 255, 255, 255), new Color(255, 0, 0, 0)),
            new SukiColorTheme("Black", new Color(255, 0, 0, 0), new Color(255, 255, 255, 255)),
        ]);
        ChangeTheme(_options.Theme);
        ChangeColorTheme(ResolveColorTheme(_options.ThemeColor));
        _initialized = true;
    }

    public void ChangeTheme(Theme theme)
    {
        _options.Theme = theme;
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
        _options.ThemeColor = colorTheme.DisplayName;
        SukiTheme.ChangeColorTheme(colorTheme);
    }

    private static SukiColorTheme ResolveColorTheme(string? displayName)
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
