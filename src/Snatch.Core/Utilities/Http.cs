using System.Net.Http.Headers;
using System.Reflection;

namespace Snatch.Core.Utilities;

public static class Http
{
    public static HttpClient Client { get; } =
        new()
        {
            DefaultRequestHeaders =
            {
                // Required by some of the services we're using
                UserAgent =
                {
                    new ProductInfoHeaderValue(
                        "Snatch",
                        Assembly.GetExecutingAssembly().GetName().Version?.ToString(3)
                    ),
                },
            },
        };
}
