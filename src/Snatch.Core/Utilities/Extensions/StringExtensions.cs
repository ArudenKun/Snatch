using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Snatch.Core.Utilities.Extensions;

public static class StringExtensions
{
    extension(string str)
    {
        public string? NullIfEmptyOrWhiteSpace() => !string.IsNullOrEmpty(str.Trim()) ? str : null;
    }

    /// <summary>
    /// Adds a char to end of given string if it does not ends with the char.
    /// </summary>
    public static string EnsureEndsWith(
        this string str,
        char c,
        StringComparison comparisonType = StringComparison.Ordinal
    )
    {
        if (str.EndsWith(c.ToString(), comparisonType))
        {
            return str;
        }

        return str + c;
    }

    /// <summary>
    /// Adds a char to beginning of given string if it does not starts with the char.
    /// </summary>
    public static string EnsureStartsWith(
        this string str,
        char c,
        StringComparison comparisonType = StringComparison.Ordinal
    )
    {
        if (str.StartsWith(c.ToString(), comparisonType))
        {
            return str;
        }

        return c + str;
    }

    /// <summary>
    /// Indicates whether this string is null or an System.String.Empty string.
    /// </summary>
    public static bool IsNullOrEmpty(
        [System.Diagnostics.CodeAnalysis.NotNullWhen(false)] this string? str
    )
    {
        return string.IsNullOrEmpty(str);
    }

    /// <summary>
    /// indicates whether this string is null, empty, or consists only of white-space characters.
    /// </summary>
    public static bool IsNullOrWhiteSpace(
        [System.Diagnostics.CodeAnalysis.NotNullWhen(false)] this string? str
    )
    {
        return string.IsNullOrWhiteSpace(str);
    }

    /// <summary>
    /// Gets a substring of a string from beginning of the string.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="str"/> is null</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="len"/> is bigger that string's length</exception>
    public static string Left(this string str, int len)
    {
        if (str.Length < len)
        {
            throw new ArgumentException(
                "len argument can not be bigger than given string's length!"
            );
        }

        return str.Substring(0, len);
    }

    /// <summary>
    /// Converts line endings in the string to <see cref="Environment.NewLine"/>.
    /// </summary>
    public static string NormalizeLineEndings(this string str)
    {
        return str.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", Environment.NewLine);
    }

    /// <summary>
    /// Gets index of nth occurrence of a char in a string.
    /// </summary>
    /// <param name="str">source string to be searched</param>
    /// <param name="c">Char to search in <paramref name="str"/></param>
    /// <param name="n">Count of the occurrence</param>
    public static int NthIndexOf(this string str, char c, int n)
    {
        var count = 0;
        for (var i = 0; i < str.Length; i++)
        {
            if (str[i] != c)
            {
                continue;
            }

            if ((++count) == n)
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// Removes first occurrence of the given postfixes from end of the given string.
    /// </summary>
    /// <param name="str">The string.</param>
    /// <param name="postFixes">one or more postfix.</param>
    /// <returns>Modified string or the same string if it has not any of given postfixes</returns>
    public static string RemovePostFix(this string str, params string[] postFixes)
    {
        return str.RemovePostFix(StringComparison.Ordinal, postFixes);
    }

    /// <summary>
    /// Removes first occurrence of the given postfixes from end of the given string.
    /// </summary>
    /// <param name="str">The string.</param>
    /// <param name="comparisonType">String comparison type</param>
    /// <param name="postFixes">one or more postfix.</param>
    /// <returns>Modified string or the same string if it has not any of given postfixes</returns>
    public static string RemovePostFix(
        this string str,
        StringComparison comparisonType,
        params string[] postFixes
    )
    {
        if (str.IsNullOrEmpty())
        {
            return str;
        }

        if (postFixes.IsNullOrEmpty())
        {
            return str;
        }

        foreach (var postFix in postFixes)
        {
            if (str.EndsWith(postFix, comparisonType))
            {
                return str.Left(str.Length - postFix.Length);
            }
        }

        return str;
    }

    /// <summary>
    /// Removes first occurrence of the given prefixes from beginning of the given string.
    /// </summary>
    /// <param name="str">The string.</param>
    /// <param name="preFixes">one or more prefix.</param>
    /// <returns>Modified string or the same string if it has not any of given prefixes</returns>
    public static string RemovePreFix(this string str, params string[] preFixes)
    {
        return str.RemovePreFix(StringComparison.Ordinal, preFixes);
    }

    /// <summary>
    /// Removes first occurrence of the given prefixes from beginning of the given string.
    /// </summary>
    /// <param name="str">The string.</param>
    /// <param name="comparisonType">String comparison type</param>
    /// <param name="preFixes">one or more prefix.</param>
    /// <returns>Modified string or the same string if it has not any of given prefixes</returns>
    public static string RemovePreFix(
        this string str,
        StringComparison comparisonType,
        params string[] preFixes
    )
    {
        if (str.IsNullOrEmpty())
        {
            return str;
        }

        if (preFixes.IsNullOrEmpty())
        {
            return str;
        }

        foreach (var preFix in preFixes)
        {
            if (str.StartsWith(preFix, comparisonType))
            {
                return str.Right(str.Length - preFix.Length);
            }
        }

        return str;
    }

    public static string ReplaceFirst(
        this string str,
        string search,
        string replace,
        StringComparison comparisonType = StringComparison.Ordinal
    )
    {
        var pos = str.IndexOf(search, comparisonType);
        if (pos < 0)
        {
            return str;
        }

        var searchLength = search.Length;
        var replaceLength = replace.Length;
        var newLength = str.Length - searchLength + replaceLength;

        Span<char> buffer = newLength <= 1024 ? stackalloc char[newLength] : new char[newLength];

        // Copy the part of the original string before the search term
        str.AsSpan(0, pos).CopyTo(buffer);

        // Copy the replacement text
        replace.AsSpan().CopyTo(buffer.Slice(pos));

        // Copy the remainder of the original string
        str.AsSpan(pos + searchLength).CopyTo(buffer.Slice(pos + replaceLength));

        return buffer.ToString();
    }

    /// <summary>
    /// Gets a substring of a string from end of the string.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="str"/> is null</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="len"/> is bigger that string's length</exception>
    public static string Right(this string str, int len)
    {
        if (str.Length < len)
        {
            throw new ArgumentException(
                "len argument can not be bigger than given string's length!"
            );
        }

        return str.Substring(str.Length - len, len);
    }

    /// <summary>
    /// Uses string.Split method to split given string by given separator.
    /// </summary>
    public static string[] Split(this string str, string separator)
    {
        return str.Split(new[] { separator }, StringSplitOptions.None);
    }

    /// <summary>
    /// Uses string.Split method to split given string by given separator.
    /// </summary>
    public static string[] Split(this string str, string separator, StringSplitOptions options)
    {
        return str.Split(new[] { separator }, options);
    }

    /// <summary>
    /// Uses string.Split method to split given string by <see cref="Environment.NewLine"/>.
    /// </summary>
    public static string[] SplitToLines(this string str)
    {
        return str.Split(Environment.NewLine);
    }

    /// <summary>
    /// Uses string.Split method to split given string by <see cref="Environment.NewLine"/>.
    /// </summary>
    public static string[] SplitToLines(this string str, StringSplitOptions options)
    {
        return str.Split(Environment.NewLine, options);
    }

    /// <summary>
    /// Converts string to enum value.
    /// </summary>
    /// <typeparam name="T">Type of enum</typeparam>
    /// <param name="value">String value to convert</param>
    /// <returns>Returns enum object</returns>
    public static T ToEnum<T>(this string value)
        where T : struct
    {
        return (T)Enum.Parse(typeof(T), value);
    }

    /// <summary>
    /// Converts string to enum value.
    /// </summary>
    /// <typeparam name="T">Type of enum</typeparam>
    /// <param name="value">String value to convert</param>
    /// <param name="ignoreCase">Ignore case</param>
    /// <returns>Returns enum object</returns>
    public static T ToEnum<T>(this string value, bool ignoreCase)
        where T : struct
    {
        return (T)Enum.Parse(typeof(T), value, ignoreCase);
    }

    public static string ToMd5(this string str)
    {
        using (var md5 = MD5.Create())
        {
            var inputBytes = Encoding.UTF8.GetBytes(str);
            var hashBytes = md5.ComputeHash(inputBytes);

            var sb = new StringBuilder();
            foreach (var hashByte in hashBytes)
            {
                sb.Append(hashByte.ToString("X2"));
            }

            return sb.ToString();
        }
    }

    public static string ToSha256(this string str)
    {
        using (var sha = SHA256.Create())
        {
            var data = sha.ComputeHash(Encoding.UTF8.GetBytes(str));

            var sb = new StringBuilder();
            foreach (var d in data)
            {
                sb.Append(d.ToString("x2"));
            }
            return sb.ToString();
        }
    }

    public static string ToSha512(this string str)
    {
        using (var sha = SHA512.Create())
        {
            var data = sha.ComputeHash(Encoding.UTF8.GetBytes(str));

            var sb = new StringBuilder();
            foreach (var d in data)
            {
                sb.Append(d.ToString("x2"));
            }
            return sb.ToString();
        }
    }

    /// <summary>
    /// Gets a substring of a string from beginning of the string if it exceeds maximum length.
    /// </summary>
    public static string? Truncate(this string? str, int maxLength)
    {
        if (str == null)
        {
            return null;
        }

        if (str.Length <= maxLength)
        {
            return str;
        }

        return str.Left(maxLength);
    }

    /// <summary>
    /// Gets a substring of a string from Ending of the string if it exceeds maximum length.
    /// </summary>
    public static string? TruncateFromBeginning(this string? str, int maxLength)
    {
        if (str == null)
        {
            return null;
        }

        if (str.Length <= maxLength)
        {
            return str;
        }

        return str.Right(maxLength);
    }

    /// <summary>
    /// Gets a substring of a string from beginning of the string if it exceeds maximum length.
    /// It adds a "..." postfix to end of the string if it's truncated.
    /// Returning string can not be longer than maxLength.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="str"/> is null</exception>
    public static string? TruncateWithPostfix(this string? str, int maxLength)
    {
        return TruncateWithPostfix(str, maxLength, "...");
    }

    /// <summary>
    /// Gets a substring of a string from beginning of the string if it exceeds maximum length.
    /// It adds given <paramref name="postfix"/> to end of the string if it's truncated.
    /// Returning string can not be longer than maxLength.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="str"/> is null</exception>
    public static string? TruncateWithPostfix(this string? str, int maxLength, string postfix)
    {
        if (str == null)
        {
            return null;
        }

        if (str == string.Empty || maxLength == 0)
        {
            return string.Empty;
        }

        if (str.Length <= maxLength)
        {
            return str;
        }

        if (maxLength <= postfix.Length)
        {
            return postfix.Left(maxLength);
        }

        return str.Left(maxLength - postfix.Length) + postfix;
    }

    /// <summary>
    /// Converts given string to a byte array using <see cref="Encoding.UTF8"/> encoding.
    /// </summary>
    public static byte[] GetBytes(this string str)
    {
        return str.GetBytes(Encoding.UTF8);
    }

    /// <summary>
    /// Converts given string to a byte array using the given <paramref name="encoding"/>
    /// </summary>
    public static byte[] GetBytes(this string str, Encoding encoding)
    {
        return encoding.GetBytes(str);
    }

    private static bool IsAllUpperCase(string input)
    {
        for (int i = 0; i < input.Length; i++)
        {
            if (Char.IsLetter(input[i]) && !Char.IsUpper(input[i]))
            {
                return false;
            }
        }

        return true;
    }
}
