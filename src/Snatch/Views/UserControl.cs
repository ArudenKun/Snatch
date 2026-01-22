using Avalonia.Controls;
using Avalonia.Interactivity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Snatch.Dependency;
using Snatch.Utilities;
using Snatch.ViewModels;

namespace Snatch.Views;

public abstract class UserControl<TViewModel> : UserControl, IView<TViewModel>, IFinalizer
    where TViewModel : ViewModel
{
    public UserControl()
    {
        MessengerConfigurator.RegisterRecipients(this);
        MessengerConfigurator.RegisterRequestors(this);
    }

    public required IServiceProvider ServiceProvider { protected get; init; }

    protected ILoggerFactory LoggerFactory => ServiceProvider.GetRequiredService<ILoggerFactory>();

    protected ILogger Logger => LoggerFactory.CreateLogger(GetType().FullName!);

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

    public void OnDestroy()
    {
        MessengerConfigurator.UnRegisterRecipients(this);
        MessengerConfigurator.UnRegisterRequestors(this);
    }
}
