using System.Text.RegularExpressions;

namespace Snatch.Core.YtDlp;

public class YtDlp
{
    private static readonly Regex RgxFile = new Regex(
        @"^outfile:\s\""?(.*)\""?",
        RegexOptions.Compiled
    );
    private static readonly Regex RgxFilePostProc = new Regex(
        @"\[download\] Destination: [a-zA-Z]:\\\S+\.\S{3,}",
        RegexOptions.Compiled
    );
}
