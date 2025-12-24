using R3;
using Serilog.Core;
using Snatch.Options;

namespace Snatch.Services;

public sealed class ObservableLoggingLevelSwitch : LoggingLevelSwitch, IDisposable
{
    private readonly CompositeDisposable _disposables = new();

    public ObservableLoggingLevelSwitch(LoggingOptions options)
        : base(options.LogEventLevel)
    {
        options
            .ObservePropertyChanged(s => s.LogEventLevel)
            .Subscribe(x => MinimumLevel = x)
            .AddTo(_disposables);
    }

    public void Dispose() => _disposables.Dispose();
}
