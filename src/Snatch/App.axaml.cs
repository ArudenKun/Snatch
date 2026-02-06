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
using ZLinq;

namespace Snatch;

public sealed class App : Application, IDisposable
{
    private readonly MainWindowViewModel _mainWindowViewModel;
    private readonly ViewLocator _viewLocator;
    private readonly ToastService _toastService;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ThemeService _themeService;
    private readonly SettingsService _settingsService;

    // private readonly ListenerLogEventSink _listenerLogEventSink;

    private IDisposable? _subscriptions;

    public App(
        MainWindowViewModel mainWindowViewModel,
        ViewLocator viewLocator,
        ToastService toastService,
        ILoggerFactory loggerFactory,
        ThemeService themeService,
        SettingsService settingsService
    // , ListenerLogEventSink listenerLogEventSink
    )
    {
        _mainWindowViewModel = mainWindowViewModel;
        _viewLocator = viewLocator;
        _toastService = toastService;
        _loggerFactory = loggerFactory;
        _themeService = themeService;
        _settingsService = settingsService;
        // _listenerLogEventSink = listenerLogEventSink;
    }

    // ReSharper disable once ArrangeModifiersOrder
    public static new App Current =>
        (App?)Application.Current
        ?? throw new InvalidOperationException("Application is not yet initialized");

    public static Window MainWindow { get; private set; } = null!;

    public static TopLevel TopLevel { get; private set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        DataTemplates.Add(_viewLocator);
        _themeService.Initialize();
        // _listenerLogEventSink.Initialize();

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
            TopLevel = MainWindow =
                window ?? throw new InvalidOperationException("Application is not yet initialized");
        }

        base.OnFrameworkInitializationCompleted();
    }

    public void Dispose()
    {
        _settingsService.Save();
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
#pragma warning disable IL2026
        var dataValidationPluginsToRemove = BindingPlugins
            .DataValidators.AsValueEnumerable()
            .OfType<DataAnnotationsValidationPlugin>()
            .ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
#pragma warning restore IL2026
    }
}
