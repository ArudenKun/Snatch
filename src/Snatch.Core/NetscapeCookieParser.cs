using System.Net;

namespace Snatch.Core;

public static class NetscapeCookieParser
{
    /// <summary>
    /// Parses a Netscape HTTP Cookie file content into a CookieCollection.
    /// </summary>
    /// <param name="fileContent">The raw string content of the cookie file.</param>
    /// <returns>A CookieCollection containing valid cookies.</returns>
    public static IEnumerable<Cookie> Parse(string fileContent)
    {
        if (string.IsNullOrWhiteSpace(fileContent))
        {
            foreach (var cookie in Enumerable.Empty<Cookie>())
            {
                yield return cookie;
            }
        }

        using var reader = new StringReader(fileContent);
        while (reader.ReadLine() is { } line)
        {
            line = line.Trim();

            // 1. Skip empty lines
            if (string.IsNullOrEmpty(line))
                continue;

            // 2. Handle comments
            // Standard comments start with '#'.
            // However, cURL/wget uses a hack: lines starting with "#HttpOnly_"
            // are valid cookies that are HttpOnly.
            var isHttpOnly = false;
            if (line.StartsWith("#HttpOnly_", StringComparison.OrdinalIgnoreCase))
            {
                isHttpOnly = true;
                // Remove the prefix to parse the rest normally
                line = line.Substring(10).TrimStart();
            }
            else if (line.StartsWith('#'))
            {
                continue; // Skip standard comments
            }

            // 3. Split by Tab
            var parts = line.Split('\t');

            // The Netscape format requires 7 columns
            if (parts.Length < 7)
                continue;

            // Column mapping:
            // 0: Domain
            // 1: Include Subdomains (TRUE/FALSE) - Not directly used by System.Net.Cookie
            // 2: Path
            // 3: Secure (TRUE/FALSE)
            // 4: Expiration (Unix Timestamp)
            // 5: Name
            // 6: Value

            var domain = parts[0];
            var path = parts[2];
            var secure = parts[3].Equals("TRUE", StringComparison.OrdinalIgnoreCase);
            var expiresUnix = long.Parse(parts[4]);
            var name = parts[5];
            var value = parts[6];

            var cookie = new Cookie(name, value, path, domain)
            {
                Secure = secure,
                HttpOnly = isHttpOnly,
                Expires = UnixTimeStampToDateTime(expiresUnix),
            };

            yield return cookie;
        }
    }

    /// <summary>
    /// Parses a file directly from a file path.
    /// </summary>
    public static IEnumerable<Cookie> ParseFile(string filePath)
    {
        return !File.Exists(filePath)
            ? throw new FileNotFoundException("Cookie file not found.", filePath)
            : Parse(File.ReadAllText(filePath));
    }

    private static DateTime UnixTimeStampToDateTime(long unixTimeStamp) =>
        DateTimeOffset.FromUnixTimeSeconds(unixTimeStamp).LocalDateTime;
}
