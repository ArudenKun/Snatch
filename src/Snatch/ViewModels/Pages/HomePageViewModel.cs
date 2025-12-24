using Lucide.Avalonia;
using Microsoft.Extensions.Logging;

namespace Snatch.ViewModels.Pages;

public sealed class HomePageViewModel : PageViewModel
{
    public override int Index => 1;
    public override LucideIconKind IconKind => LucideIconKind.House;

    public int Test { get; } = Random.Shared.Next(1, 1000);

    public override void OnLoaded()
    {
        Logger.LogInformation($"{nameof(HomePageViewModel)} {nameof(OnLoaded)}");
    }

    public override void OnUnloaded()
    {
        Logger.LogInformation($"{nameof(HomePageViewModel)} {nameof(OnUnloaded)}");
    }

    private void Reset() { }
}
