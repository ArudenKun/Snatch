using AutoInterfaceAttributes;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using R3;
using Snatch.Services;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Local;

namespace Snatch.ViewModels;

[AutoInterface(Inheritance = [typeof(IDisposable)])]
public abstract partial class ViewModel : ObservableValidator, IViewModel
{
    public required IAbpLazyServiceProvider LazyServiceProvider { protected get; init; }

    protected ILoggerFactory LoggerFactory =>
        LazyServiceProvider.LazyGetRequiredService<ILoggerFactory>();

    protected ILogger Logger =>
        LazyServiceProvider.LazyGetService<ILogger>(_ =>
            LoggerFactory.CreateLogger(GetType().FullName!)
        );

    public ILocalEventBus LocalEventBus =>
        LazyServiceProvider.LazyGetRequiredService<ILocalEventBus>();

    public IToastService ToastService =>
        LazyServiceProvider.LazyGetRequiredService<IToastService>();

    public IDialogService DialogService =>
        LazyServiceProvider.LazyGetRequiredService<IDialogService>();

    public SettingsService SettingsService =>
        LazyServiceProvider.LazyGetRequiredService<SettingsService>();

    public IThemeService ThemeService =>
        LazyServiceProvider.LazyGetRequiredService<IThemeService>();

    public IStorageProvider StorageProvider =>
        LazyServiceProvider.LazyGetRequiredService<IStorageProvider>();

    public IClipboard Clipboard => LazyServiceProvider.LazyGetRequiredService<IClipboard>();
    public ILauncher Launcher => LazyServiceProvider.LazyGetRequiredService<ILauncher>();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    public virtual partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string IsBusyText { get; set; } = string.Empty;

    public bool IsNotBusy => !IsBusy;

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
        catch (Exception ex) when (LogException(ex, true, showException)) { }
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
            // ToastService.ShowExceptionToast(ex, "Error", ex.ToStringDemystified());
        }

        return shouldCatch;
    }

    #region Disposal

    // ReSharper disable once CollectionNeverQueried.Local
    private readonly CompositeDisposable _disposables = new();
    protected bool IsDisposed { get; private set; }

    ~ViewModel() => Dispose(false);

    /// <inheritdoc />>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public void AddTo(IDisposable disposable)
    {
        if (IsDisposed)
        {
            disposable.Dispose();
            return;
        }

        _disposables.Add(disposable);
    }

    /// <inheritdoc cref="Dispose"/>>
    protected virtual void Dispose(bool disposing)
    {
        if (IsDisposed)
            return;

        if (!disposing)
            return;

        _disposables.Dispose();
        IsDisposed = true;
    }

    #endregion
}
