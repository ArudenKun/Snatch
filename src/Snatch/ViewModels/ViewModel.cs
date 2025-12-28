using System.Diagnostics;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using R3;
using Snatch.Options;
using Snatch.Services;

namespace Snatch.ViewModels;

public abstract partial class ViewModel : ObservableValidator, IDisposable
{
    protected ViewModel()
    {
        MessengerRegistrator.Register(this);
    }

    public required IServiceProvider ServiceProvider { protected get; init; }

    protected ILoggerFactory LoggerFactory => ServiceProvider.GetRequiredService<ILoggerFactory>();

    protected ILogger Logger => LoggerFactory.CreateLogger(GetType().FullName!);

    protected IMessenger Messenger => ServiceProvider.GetRequiredService<IMessenger>();

    protected ToastService ToastService => ServiceProvider.GetRequiredService<ToastService>();

    protected DialogService DialogService => ServiceProvider.GetRequiredService<DialogService>();

    protected SettingsService SettingsService =>
        ServiceProvider.GetRequiredService<SettingsService>();

    protected ThemeService ThemeService => ServiceProvider.GetRequiredService<ThemeService>();

    public GeneralOptions GeneralOptions =>
        ServiceProvider.GetRequiredService<IOptions<GeneralOptions>>().Value;

    public AppearanceOptions AppearanceOptions =>
        ServiceProvider.GetRequiredService<IOptions<AppearanceOptions>>().Value;

    public LoggingOptions LoggingOptions =>
        ServiceProvider.GetRequiredService<IOptions<LoggingOptions>>().Value;

    public YoutubeOptions YoutubeOptions =>
        ServiceProvider.GetRequiredService<IOptions<YoutubeOptions>>().Value;

    public IStorageProvider StorageProvider =>
        ServiceProvider.GetRequiredService<IStorageProvider>();

    public IClipboard Clipboard => ServiceProvider.GetRequiredService<IClipboard>();
    public ILauncher Launcher => ServiceProvider.GetRequiredService<ILauncher>();

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

        // Logger.LogException(ex);
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

    public void AddTo(IDisposable disposable)
    {
        if (_disposed)
        {
            disposable.Dispose();
            return;
        }

        _disposables.Add(disposable);
    }

    ~ViewModel() => Dispose(false);

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

    #endregion
}
