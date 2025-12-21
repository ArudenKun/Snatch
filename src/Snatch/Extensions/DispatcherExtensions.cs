using Avalonia.Threading;

namespace Snatch.Extensions;

public static class DispatcherExtensions
{
    extension(IDispatcher dispatcher)
    {
        public Task<T> PostAsync<T>(Func<T> action, DispatcherPriority dispatcherPriority = default)
        {
            var tcs = new TaskCompletionSource<T>();
            dispatcher.Post(() => tcs.SetResult(action()), dispatcherPriority);
            return tcs.Task;
        }
    }

    extension(Task task)
    {
        public void WaitOnDispatcherFrame(Dispatcher? dispatcher = null)
        {
            var frame = new DispatcherFrame();
            AggregateException? capturedException = null;

            task.ContinueWith(
                t =>
                {
                    capturedException = t.Exception;
                    frame.Continue = false; // 结束消息循环
                },
                TaskContinuationOptions.AttachedToParent
            );

            dispatcher ??= Dispatcher.UIThread;
            dispatcher.PushFrame(frame);

            if (capturedException != null)
            {
                throw capturedException;
            }
        }
    }
}
