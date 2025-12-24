using Avalonia.Collections;
using Lucide.Avalonia;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.DependencyInjection;
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

    public int Test { get; } = Random.Shared.Next(1, 1000);
}
