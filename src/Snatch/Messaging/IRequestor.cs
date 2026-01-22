using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Snatch.Messaging;

public interface IRequestor<in TRequest, T>
    where TRequest : RequestMessage<T>
    where T : class
{
    void Request(object receiver, TRequest message);
}
