using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Snatch.Models.EventData;
using SukiUI.Dialogs;
using SukiUI.Toasts;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace Snatch.ViewModels;

public sealed partial class MainWindowViewModel
    : ViewModel,
        ILocalEventHandler<SplashViewFinishedEventData>,
        ISingletonDependency
{
    private readonly IServiceProvider _serviceProvider;

    public MainWindowViewModel(
        ISukiToastManager toastManager,
        ISukiDialogManager dialogManager,
        IServiceProvider serviceProvider
    )
    {
        ToastManager = toastManager;
        DialogManager = dialogManager;
        _serviceProvider = serviceProvider;
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
        ContentViewModel = _serviceProvider.GetRequiredService<SplashViewModel>();
    }

    [RelayCommand(CanExecute = nameof(IsMainView))]
    private async Task ShowPageAsync(Type pageType)
    {
        await LocalEventBus.PublishAsync(new ShowPageEventData(pageType));
    }

    public Task HandleEventAsync(SplashViewFinishedEventData eventData)
    {
        ContentViewModel = _serviceProvider.GetRequiredService<MainViewModel>();
        return Task.CompletedTask;
    }

    #region Update

    private void Update() { }

    #endregion
}
