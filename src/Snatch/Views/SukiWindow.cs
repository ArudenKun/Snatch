using Avalonia.Interactivity;
using Snatch.Utilities;
using Snatch.ViewModels;
using SukiUI.Controls;
using Volo.Abp.DependencyInjection;

namespace Snatch.Views;

public abstract class SukiWindow<TViewModel> : SukiWindow, IView<TViewModel>
    where TViewModel : ViewModel
{
    public new TViewModel DataContext
    {
        get =>
            base.DataContext as TViewModel
            ?? throw new InvalidCastException(
                $"DataContext is null or not of the expected type '{typeof(TViewModel).FullName}'."
            );
        set => base.DataContext = value;
    }

    public TViewModel ViewModel => DataContext;

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        DispatchHelper.Invoke(() => ViewModel.OnLoaded());
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        DispatchHelper.Invoke(() => ViewModel.OnUnloaded());
    }
}
