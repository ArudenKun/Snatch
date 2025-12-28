using AsyncAwaitBestPractices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Humanizer;
using Snatch.Models.Messages;

namespace Snatch.ViewModels.Components;

public sealed partial class SplashViewModel : ViewModel
{
    [ObservableProperty]
    public partial string StatusText { get; set; } = "Initializing";

    public override void OnLoaded()
    {
        StartAsync().SafeFireAndForget();
    }

    private async Task StartAsync()
    {
        await Task.Delay(1.Seconds());
        StatusText = "Loading Settings";
        await Task.Delay(200.Milliseconds());
        Messenger.Send(new SplashViewFinishedMessage());

        if (GeneralOptions.ShowConsole)
        {
            Messenger.Send(new ConsoleWindowShowMessage());
        }
    }
}
