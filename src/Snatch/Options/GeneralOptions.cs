using CommunityToolkit.Mvvm.ComponentModel;

namespace Snatch.Options;

public sealed partial class GeneralOptions : ObservableObject
{
    [ObservableProperty]
    public partial bool AutoUpdate { get; set; } = false;
}
