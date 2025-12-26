using System.Text;

namespace Snatch.Core.Utilities.Extensions;

public static class PathExtensions
{
    extension(Path)
    {
        public static string EscapeFileName(string path)
        {
            var buffer = new StringBuilder(path.Length);

            foreach (var c in path)
                buffer.Append(!Path.GetInvalidFileNameChars().Contains(c) ? c : '_');

            return buffer.ToString();
        }
    }
}
