using Avalonia.Collections;
using Microsoft.Extensions.DependencyInjection;
using Snatch.Models;
using Snatch.Utilities;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace Snatch.ViewModels;

[Dependency(ServiceLifetime.Singleton)]
public sealed class ConsoleWindowViewModel : ViewModel, ILocalEventHandler<LogEntry>
{
    public IAvaloniaList<LogEntry> Entries { get; } = new AvaloniaList<LogEntry>();

    public Task HandleEventAsync(LogEntry eventData)
    {
        DispatchHelper.Invoke(() => Entries.Add(eventData));
        return Task.CompletedTask;
    }
}
