namespace Snatch.Core.Utilities;

public sealed class ResizableSemaphore : IDisposable
{
    private readonly Lock _lock = new();
    private readonly Queue<TaskCompletionSource> _waiters = new();
    private readonly CancellationTokenSource _cts = new();

    private bool _isDisposed;
    private int _count;

    public int MaxCount
    {
        get
        {
            lock (_lock)
            {
                return field;
            }
        }
        set
        {
            lock (_lock)
            {
                field = value;
                Refresh();
            }
        }
    } = int.MaxValue;

    private void Refresh()
    {
        lock (_lock)
        {
            // Provide access to pending waiters, as long as max count allows
            while (_count < MaxCount && _waiters.TryDequeue(out var waiter))
            {
                // Don't increment the count if the waiter has already been
                // completed before (most likely by getting canceled).
                if (waiter.TrySetResult())
                    _count++;
            }
        }
    }

    public async Task<IDisposable> AcquireAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        var waiter = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        await using (_cts.Token.Register(() => waiter.TrySetCanceled(_cts.Token)))
        await using (cancellationToken.Register(() => waiter.TrySetCanceled(cancellationToken)))
        {
            // Add the waiter to the queue
            lock (_lock)
            {
                _waiters.Enqueue(waiter);
                Refresh();
            }

            // Wait until this waiter has been given access
            await waiter.Task;

            return new AcquiredAccess(this);
        }
    }

    private void Release()
    {
        lock (_lock)
        {
            _count--;
            Refresh();
        }
    }

    public void Dispose()
    {
        if (!_isDisposed)
        {
            _cts.Cancel();
            _cts.Dispose();
        }

        _isDisposed = true;
    }

    private class AcquiredAccess : IDisposable
    {
        private readonly ResizableSemaphore _semaphore;

        private bool _isDisposed;

        public AcquiredAccess(ResizableSemaphore semaphore)
        {
            _semaphore = semaphore;
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _semaphore.Release();
            }

            _isDisposed = true;
        }
    }
}
