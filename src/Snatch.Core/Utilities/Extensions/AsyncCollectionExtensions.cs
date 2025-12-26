using System.Runtime.CompilerServices;

namespace Snatch.Core.Utilities.Extensions;

public static class AsyncCollectionExtensions
{
    extension<T>(IAsyncEnumerable<T> asyncEnumerable)
    {
        private async ValueTask<IReadOnlyList<T>> CollectAsync()
        {
            var list = new List<T>();

            await foreach (var i in asyncEnumerable)
                list.Add(i);

            return list;
        }

        public ValueTaskAwaiter<IReadOnlyList<T>> GetAwaiter() =>
            asyncEnumerable.CollectAsync().GetAwaiter();
    }
}
