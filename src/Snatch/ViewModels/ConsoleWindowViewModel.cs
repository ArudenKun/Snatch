using Avalonia.Collections;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Snatch.Models;
using Snatch.Utilities;
using Volo.Abp.DependencyInjection;

namespace Snatch.ViewModels;

[Dependency(ServiceLifetime.Singleton)]
public sealed class ConsoleWindowViewModel : ViewModel, IRecipient<LogEntry>
{
    public IAvaloniaList<LogEntry> Entries { get; } = new AvaloniaList<LogEntry>();

    public void Receive(LogEntry message)
    {
        DispatchHelper.Invoke(() => Entries.Add(message));
    }
}
