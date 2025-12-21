namespace Snatch.Core;

public static class Singleton<T>
    where T : new()
{
    private static readonly Lazy<T> Lazy = new(() => new T());

    public static T Instance => Lazy.Value;
}
