using Avalonia.Collections;
using Lucide.Avalonia;
using Volo.Abp.DependencyInjection;
using ZLinq;

namespace Snatch.ViewModels.Pages;

public sealed partial class SettingsPageViewModel : PageViewModel, ISingletonDependency
{
    public SettingsPageViewModel()
    {
        IsVisibleOnSideMenu = false;
    }

    public override int Index => int.MaxValue;
    public override string DisplayName => "Settings";
    public override LucideIconKind IconKind => LucideIconKind.Settings;

    public IAvaloniaReadOnlyList<string> ColorThemes =>
        new AvaloniaList<string>(
            ThemeService.ColorThemes.AsValueEnumerable().Select(x => x.DisplayName).ToList()
        );
}
