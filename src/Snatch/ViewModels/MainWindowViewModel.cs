using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Snatch.Dependency;
using Snatch.Messaging.Messages;
using SukiUI.Dialogs;
using SukiUI.Toasts;

namespace Snatch.ViewModels;

[Dependency(ServiceLifetime.Singleton)]
public sealed partial class MainWindowViewModel : ViewModel, IRecipient<SplashViewFinishedMessage>
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
        ContentViewModel = ServiceProvider.GetRequiredService<Components.SplashViewModel>();
    }

    [RelayCommand(CanExecute = nameof(IsMainView))]
    private Task ShowPageAsync(Type pageType)
    {
        Messenger.Send(new ShowPageMessage(pageType));
        return Task.CompletedTask;
    }

    public void Receive(SplashViewFinishedMessage message)
    {
        ContentViewModel = ServiceProvider.GetRequiredService<MainViewModel>();
    }
}
