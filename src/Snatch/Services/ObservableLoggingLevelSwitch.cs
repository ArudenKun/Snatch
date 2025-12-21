using R3;
using R3.ObservableEvents;
using Serilog.Core;

namespace Snatch.Services;

public sealed class ObservableLoggingLevelSwitch : LoggingLevelSwitch, IDisposable
{
    private readonly CompositeDisposable _disposables = new();

    public ObservableLoggingLevelSwitch(SettingsService settingsService)
        : base(settingsService.Logging.LogEventLevel)
    {
        settingsService
            .Events()
            .Loaded.Take(1)
            .Subscribe(_ =>
                settingsService
                    .Logging.ObservePropertyChanged(s => s.LogEventLevel)
                    .Subscribe(x => MinimumLevel = x)
                    .AddTo(_disposables)
            )
            .AddTo(_disposables);
    }

    public void Dispose() => _disposables.Dispose();
}
