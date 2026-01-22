using Microsoft.Extensions.Hosting;

namespace Snatch.Services;

public sealed class LifetimeHostedService : IHostedService
{
    // private readonly SettingsService _settingsService;
    //
    // public LifetimeHostedService(SettingsService settingsService)
    // {
    //     _settingsService = settingsService;
    // }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        // await _settingsService.SaveAsync();
    }
}
