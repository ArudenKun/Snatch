namespace Snatch.Core.Utilities.Extensions;

public static class CollectionExtensions
{
    extension<T>(ICollection<T>? source)
    {
        public void AddRange(IEnumerable<T> items)
        {
            if (source.IsNullOrEmpty())
                return;

            foreach (var i in items)
                source!.Add(i);
        }

        /// <summary>
        /// Checks whatever given collection object is null or has no item.
        /// </summary>
        public bool IsNullOrEmpty()
        {
            return source is not { Count: > 0 };
        }
    }
}
