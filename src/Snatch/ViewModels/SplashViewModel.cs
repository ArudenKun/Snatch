using CommunityToolkit.Mvvm.ComponentModel;
using Humanizer;
using Snatch.Models.EventData;
using Volo.Abp.DependencyInjection;

namespace Snatch.ViewModels;

public sealed partial class SplashViewModel : ViewModel, ITransientDependency
{
    [ObservableProperty]
    public partial string StatusText { get; set; } = "Initializing";

    public override async void OnLoaded()
    {
        await Task.Delay(1.Seconds());
        StatusText = "Loading Settings";
        await Task.Delay(200.Milliseconds());
        await LocalEventBus.PublishAsync(new SplashViewFinishedEventData());
    }
}
