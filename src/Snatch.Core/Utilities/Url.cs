using System.Text.RegularExpressions;
using Snatch.Core.Utilities.Extensions;

namespace Snatch.Core.Utilities;

public static class Url
{
    public static string? TryExtractFileName(string url) =>
        Regex.Match(url, @".+/([^?]*)").Groups[1].Value.NullIfEmptyOrWhiteSpace();
}
