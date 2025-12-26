using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Snatch.Models.EventData;
using SukiUI.Dialogs;
using SukiUI.Toasts;
using Volo.Abp.DependencyInjection;

namespace Snatch.ViewModels;

[Dependency(ServiceLifetime.Singleton)]
public sealed partial class MainWindowViewModel
    : ViewModel,
        // ILocalEventHandler<SplashViewFinishedEventData>,
        IRecipient<SplashViewFinishedEventData>,
        ISingletonDependency
{
    public MainWindowViewModel(ISukiToastManager toastManager, ISukiDialogManager dialogManager)
    {
        ToastManager = toastManager;
        DialogManager = dialogManager;
    }

    public ISukiToastManager ToastManager { get; }
    public ISukiDialogManager DialogManager { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMainView))]
    [NotifyCanExecuteChangedFor(nameof(ShowPageCommand))]
    public partial ViewModel ContentViewModel { get; set; } = null!;

    public bool IsMainView => ContentViewModel is MainViewModel;

    public override void OnLoaded()
    {
        ContentViewModel = ServiceProvider.GetRequiredService<SplashViewModel>();
    }

    [RelayCommand(CanExecute = nameof(IsMainView))]
    private Task ShowPageAsync(Type pageType)
    {
        // await LocalEventBus.PublishAsync(new ShowPageEventData(pageType));
        Messenger.Send(new ShowPageEventData(pageType));
        return Task.CompletedTask;
    }

    public Task HandleEventAsync(SplashViewFinishedEventData eventData)
    {
        ContentViewModel = ServiceProvider.GetRequiredService<MainViewModel>();
        return Task.CompletedTask;
    }

    public void Receive(SplashViewFinishedEventData message)
    {
        ContentViewModel = ServiceProvider.GetRequiredService<MainViewModel>();
    }
}
