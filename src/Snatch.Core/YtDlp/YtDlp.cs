using System.Text.RegularExpressions;

namespace Snatch.Core.YtDlp;

public partial class YtDlp
{
    [GeneratedRegex(@"^outfile:\s\""?(.*)\""?", RegexOptions.Compiled)]
    private partial Regex RgxFile { get; }

    [GeneratedRegex(@"\[download\] Destination: [a-zA-Z]:\\\S+\.\S{3,}", RegexOptions.Compiled)]
    private partial Regex RgxFilePostProc { get; }
}
