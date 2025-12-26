using YoutubeExplode.Common;

namespace Snatch.Core.Utilities.Extensions;

public static class YoutubeExtensions
{
    extension(Thumbnail thumbnail)
    {
        public string? TryGetImageFormat() =>
            Url.TryExtractFileName(thumbnail.Url)?.Pipe(Path.GetExtension)?.Trim('.');
    }
}
