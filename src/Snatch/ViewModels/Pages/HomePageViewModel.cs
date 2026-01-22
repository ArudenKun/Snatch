using CommunityToolkit.Mvvm.Input;
using Lucide.Avalonia;
using Microsoft.Extensions.Logging;

namespace Snatch.ViewModels.Pages;

public sealed partial class HomePageViewModel : PageViewModel
{
    public override int Index => 1;
    public override LucideIconKind IconKind => LucideIconKind.House;

    public int Test { get; private set; } = Random.Shared.Next(1, 1000);

    public override void OnLoaded()
    {
        Test = Random.Shared.Next(0, 100);
        OnPropertyChanged(nameof(Test));
        DataService.Test = Guid.CreateVersion7().ToString();
        Logger.LogInformation($"{nameof(HomePageViewModel)} {nameof(OnLoaded)}");
    }

    public override void OnUnloaded()
    {
        Logger.LogInformation($"{nameof(HomePageViewModel)} {nameof(OnUnloaded)}");
    }

    [RelayCommand]
    private void Log()
    {
        var randomLevel = (LogLevel)Random.Shared.Next(0, 6);
        Logger.Log(randomLevel, "{Guid}", Guid.CreateVersion7());
    }
}
