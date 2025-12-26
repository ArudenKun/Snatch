namespace Snatch.Core.Utilities.Extensions;

public static class GenericExtensions
{
    extension<TIn>(TIn input)
    {
        public TOut Pipe<TOut>(Func<TIn, TOut> transform) => transform(input);
    }
}
