using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using R3;
using Serilog.Core;
using Snatch.Messaging.Messages;
using Snatch.Options;

namespace Snatch.Services;

public sealed class ObservableLoggingLevelSwitch
    : LoggingLevelSwitch,
        IDisposable,
        IRecipient<SplashViewFinishedMessage>
{
    private readonly IServiceProvider _serviceProvider;
    private IDisposable? _subscription;

    public ObservableLoggingLevelSwitch(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;

        MessengerConfigurator.RegisterRecipients(this);
    }

    public void Receive(SplashViewFinishedMessage message)
    {
        _subscription = _serviceProvider
            .GetRequiredService<SettingsService>()
            .Get<LoggingOptions>()
            .ObservePropertyChanged(s => s.LogEventLevel)
            .Subscribe(x => MinimumLevel = x);
    }

    public void Dispose()
    {
        MessengerConfigurator.UnRegisterRecipients(this);
        _subscription?.Dispose();
    }
}
