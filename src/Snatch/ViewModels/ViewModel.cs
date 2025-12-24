using System.Diagnostics;
using AutoInterfaceAttributes;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using R3;
using Snatch.Options;
using Snatch.Services;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Local;

namespace Snatch.ViewModels;

[AutoInterface(Inheritance = [typeof(IDisposable), typeof(ITransientDependency)])]
public abstract partial class ViewModel : ObservableValidator, IViewModel
{
    public required IServiceProvider ServiceProvider { protected get; init; }
    public required ITransientCachedServiceProvider CachedServiceProvider { protected get; init; }

    protected ILoggerFactory LoggerFactory =>
        CachedServiceProvider.GetRequiredService<ILoggerFactory>();

    protected ILogger Logger =>
        CachedServiceProvider.GetService<ILogger>(_ =>
            LoggerFactory.CreateLogger(GetType().FullName!)
        );

    public ILocalEventBus LocalEventBus =>
        CachedServiceProvider.GetRequiredService<ILocalEventBus>();

    public IToastService ToastService => CachedServiceProvider.GetRequiredService<IToastService>();

    public IDialogService DialogService =>
        CachedServiceProvider.GetRequiredService<IDialogService>();

    public SettingsService SettingsService =>
        CachedServiceProvider.GetRequiredService<SettingsService>();

    public GeneralOptions GeneralOptions =>
        CachedServiceProvider.GetRequiredService<IOptions<GeneralOptions>>().Value;

    public AppearanceOptions AppearanceOptions =>
        CachedServiceProvider.GetRequiredService<IOptions<AppearanceOptions>>().Value;

    public LoggingOptions LoggingOptions =>
        CachedServiceProvider.GetRequiredService<IOptions<LoggingOptions>>().Value;

    public YoutubeOptions YoutubeOptions =>
        CachedServiceProvider.GetRequiredService<IOptions<YoutubeOptions>>().Value;

    public IStorageProvider StorageProvider =>
        CachedServiceProvider.GetRequiredService<IStorageProvider>();

    public IClipboard Clipboard => CachedServiceProvider.GetRequiredService<IClipboard>();
    public ILauncher Launcher => CachedServiceProvider.GetRequiredService<ILauncher>();

    [ObservableProperty]
    public virtual partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string IsBusyText { get; set; } = string.Empty;

    public virtual void OnLoaded() { }

    public virtual void OnUnloaded() { }

    protected void OnAllPropertiesChanged() => OnPropertyChanged(string.Empty);

    public async Task SetBusyAsync(Func<Task> func, string busyText = "", bool showException = true)
    {
        IsBusy = true;
        IsBusyText = busyText;
        try
        {
            await func();
        }
        catch (Exception ex) when (LogException(ex, true, showException))
        {
            // Not Used
        }
        finally
        {
            IsBusy = false;
            IsBusyText = string.Empty;
        }
    }

    public bool LogException(Exception? ex, bool shouldCatch = false, bool shouldDisplay = false)
    {
        if (ex is null)
        {
            return shouldCatch;
        }

        Logger.LogException(ex);
        if (shouldDisplay)
        {
            ToastService.ShowExceptionToast(ex, "Error", ex.ToStringDemystified());
        }

        return shouldCatch;
    }

    #region Disposal

    // ReSharper disable once CollectionNeverQueried.Local
    private readonly CompositeDisposable _disposables = new();
    private bool _disposed;

    ~ViewModel() => Dispose(false);

    /// <inheritdoc />>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc cref="Dispose"/>>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            _disposables.Dispose();
        }

        _disposed = true;
    }

    public void AddTo(IDisposable disposable)
    {
        if (_disposed)
        {
            disposable.Dispose();
            return;
        }

        _disposables.Add(disposable);
    }

    #endregion
}
