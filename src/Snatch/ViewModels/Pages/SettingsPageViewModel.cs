using Avalonia.Collections;
using Lucide.Avalonia;
using Microsoft.Extensions.DependencyInjection;
using Snatch.Dependency;
using ZLinq;

namespace Snatch.ViewModels.Pages;

[Dependency(ServiceLifetime.Singleton)]
public sealed class SettingsPageViewModel : PageViewModel
{
    public SettingsPageViewModel()
    {
        IsVisibleOnSideMenu = false;
    }

    public override int Index => int.MaxValue;
    public override LucideIconKind IconKind => LucideIconKind.Settings;

    public IAvaloniaReadOnlyList<string> ColorThemes =>
        new AvaloniaList<string>(
            ThemeService.ColorThemes.AsValueEnumerable().Select(x => x.DisplayName).ToList()
        );

    public int Test { get; } = Random.Shared.Next(1, 1000);
}
