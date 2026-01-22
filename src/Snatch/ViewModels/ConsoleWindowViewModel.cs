using Avalonia.Collections;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.Extensions.DependencyInjection;
using Snatch.Dependency;
using Snatch.Messaging;
using Snatch.Messaging.Messages;
using Snatch.Models;
using Snatch.Utilities;

namespace Snatch.ViewModels;

[Dependency(ServiceLifetime.Singleton)]
public sealed class ConsoleWindowViewModel
    : ViewModel,
        IRecipient<LogMessage>,
        IRequestor<TestRequestMessage, string>
{
    public IAvaloniaList<LogMessage> Entries { get; } = new AvaloniaList<LogMessage>();

    public void Receive(LogMessage message)
    {
        DispatchHelper.Invoke(() => Entries.Add(message));
    }

    public void Request(object receiver, TestRequestMessage message)
    {
        var instance = (ConsoleWindowViewModel)receiver;
    }
}
