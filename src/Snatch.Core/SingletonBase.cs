namespace Snatch.Core;

public abstract class SingletonBase<TSelf>
    where TSelf : new()
{
    private static readonly Lazy<TSelf> Lazy = new(() => new TSelf());

    public static TSelf Instance => Lazy.Value;
}
