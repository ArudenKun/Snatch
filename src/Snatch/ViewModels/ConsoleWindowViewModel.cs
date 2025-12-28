using Avalonia.Collections;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Snatch.Dependency;
using Snatch.Models;
using Snatch.Models.Messages;
using Snatch.Utilities;

namespace Snatch.ViewModels;

[Dependency(ServiceLifetime.Singleton)]
public sealed class ConsoleWindowViewModel : ViewModel, IRecipient<LogMessage>
{
    public IAvaloniaList<LogMessage> Entries { get; } = new AvaloniaList<LogMessage>();

    public void Receive(LogMessage message)
    {
        DispatchHelper.Invoke(() => Entries.Add(message));
    }
}
