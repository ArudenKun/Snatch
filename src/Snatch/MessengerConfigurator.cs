using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using ServiceScan.SourceGenerator;
using Snatch.Messaging;

namespace Snatch;

public static partial class MessengerConfigurator
{
    private static IMessenger Messenger => StrongReferenceMessenger.Default;

    [GenerateServiceRegistrations(
        AssignableTo = typeof(IRecipient<>),
        CustomHandler = nameof(RegisterRecipientsHandler)
    )]
    public static partial void RegisterRecipients(object instance);

    private static void RegisterRecipientsHandler<TRecipient, TMessage>(object instance)
        where TRecipient : class, IRecipient<TMessage>
        where TMessage : class
    {
        if (instance is TRecipient recipient)
        {
            Messenger.Register(recipient);
        }
    }

    [GenerateServiceRegistrations(
        AssignableTo = typeof(IRequestor<,>),
        CustomHandler = nameof(RegisterRequestorsHandler)
    )]
    public static partial void RegisterRequestors(object instance);

    private static void RegisterRequestorsHandler<TRequestor, TRequest, T>(object instance)
        where TRequestor : class, IRequestor<TRequest, T>
        where TRequest : RequestMessage<T>
        where T : class
    {
        if (instance is TRequestor requestor)
        {
            Messenger.Register<TRequestor, TRequest>(
                requestor,
                (receiver, message) => receiver.Request(receiver, message)
            );
        }
    }

    [GenerateServiceRegistrations(
        AssignableTo = typeof(IRecipient<>),
        CustomHandler = nameof(UnRegisterRecipientsHandler)
    )]
    public static partial void UnRegisterRecipients(object instance);

    private static void UnRegisterRecipientsHandler<TRecipient, TMessage>(object instance)
        where TRecipient : class, IRecipient<TMessage>
        where TMessage : class
    {
        if (instance is TRecipient recipient)
        {
            Messenger.Unregister<TMessage>(recipient);
        }
    }

    [GenerateServiceRegistrations(
        AssignableTo = typeof(IRequestor<,>),
        CustomHandler = nameof(UnRegisterRequestorsHandler)
    )]
    public static partial void UnRegisterRequestors(object instance);

    private static void UnRegisterRequestorsHandler<TRequestor, TRequest, T>(object instance)
        where TRequestor : class, IRequestor<TRequest, T>
        where TRequest : RequestMessage<T>
        where T : class
    {
        if (instance is TRequestor requestor)
        {
            Messenger.Unregister<TRequest>(requestor);
        }
    }
}
