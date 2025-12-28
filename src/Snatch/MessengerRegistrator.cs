using CommunityToolkit.Mvvm.Messaging;
using ServiceScan.SourceGenerator;

namespace Snatch;

public static partial class MessengerRegistrator
{
    [GenerateServiceRegistrations(
        AssignableTo = typeof(IRecipient<>),
        CustomHandler = nameof(RegisterHandler)
    )]
    public static partial void Register(object instance);

    private static void RegisterHandler<TRecipient, TMessage>(object instance)
        where TRecipient : class, IRecipient<TMessage>
        where TMessage : class
    {
        if (instance is TRecipient recipient)
        {
            WeakReferenceMessenger.Default.Register(recipient);
        }
    }
}
