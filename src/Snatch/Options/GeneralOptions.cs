using CommunityToolkit.Mvvm.ComponentModel;
using Snatch.Utilities;
using Snatch.Utilities.Extensions;

namespace Snatch.Options;

[Option("General", 1)]
public sealed partial class GeneralOptions : ObservableObject
{
    [ObservableProperty]
    public partial bool AutoUpdate { get; set; } = false;

    [ObservableProperty]
    public partial bool ShowConsole { get; set; } = false;

    // public ConnectionStrings ConnectionStrings { get; set; } =
    //     new() { Default = $"Data Source={AppHelper.DataDir.CombinePath($"{AppHelper.Name}.db")}" };
}
