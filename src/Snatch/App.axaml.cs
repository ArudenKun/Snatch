using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Microsoft.Extensions.Logging;
using R3;
using R3.ObservableEvents;
using Snatch.Services;
using Snatch.Utilities;
using Snatch.ViewModels;
using Volo.Abp.DependencyInjection;
using ZLinq;

namespace Snatch;

public sealed class App : Application, IDisposable, ISingletonDependency
{
    private readonly MainWindowViewModel _mainWindowViewModel;
    private readonly ViewLocator _viewLocator;
    private readonly IToastService _toastService;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IThemeService _themeService;

    private IDisposable? _subscriptions;

    public App(
        MainWindowViewModel mainWindowViewModel,
        ViewLocator viewLocator,
        IToastService toastService,
        ILoggerFactory loggerFactory,
        IThemeService themeService
    )
    {
        _mainWindowViewModel = mainWindowViewModel;
        _viewLocator = viewLocator;
        _toastService = toastService;
        _loggerFactory = loggerFactory;
        _themeService = themeService;
    }

    // ReSharper disable once ArrangeModifiersOrder
    public static new App Current =>
        (App?)Application.Current
        ?? throw new ArgumentNullException(
            nameof(Application.Current),
            "Application is not yet initialized"
        );

    public static TopLevel TopLevel { get; private set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        DataTemplates.Add(_viewLocator);
        _themeService.Initialize();

        _subscriptions = Disposable.Combine(
            AppDomain
                .CurrentDomain.Events()
                .UnhandledException.Subscribe(e =>
                    HandleUnhandledException((Exception)e.ExceptionObject, AppHelper.Name)
                ),
            RxEvents.TaskSchedulerUnobservedTaskException.Subscribe(e =>
            {
                HandleUnhandledException(e.Exception, $"{AppHelper.Name} Task");
                e.SetObserved();
            }),
            Dispatcher
                .UIThread.Events()
                .UnhandledException.Subscribe(e =>
                {
                    HandleUnhandledException(e.Exception, $"{AppHelper.Name} UI");
                    e.Handled = true;
                })
        );
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit.
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();
            var window = _viewLocator.CreateView(_mainWindowViewModel) as Window;
            desktop.MainWindow = window;
            TopLevel =
                window
                ?? throw new ArgumentNullException(
                    nameof(Application.Current),
                    "Application is not yet initialized"
                );
        }

        base.OnFrameworkInitializationCompleted();
    }

    public void Dispose()
    {
        _subscriptions?.Dispose();
    }

    private void HandleUnhandledException(Exception exception, string category)
    {
        var logger = _loggerFactory.CreateLogger(category);
        logger.LogError(exception, "Unhandled Exception");
        DispatchHelper.Invoke(() =>
            _toastService.ShowExceptionToast(exception, $"{category} Exception")
        );
    }

    private static void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove = BindingPlugins
            .DataValidators.AsValueEnumerable()
            .OfType<DataAnnotationsValidationPlugin>()
            .ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}
