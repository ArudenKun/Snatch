using CommunityToolkit.Mvvm.ComponentModel;

namespace Snatch.Options;

[Option("General")]
public sealed partial class GeneralOptions : ObservableObject
{
    [ObservableProperty]
    public partial bool AutoUpdate { get; set; } = false;
}
