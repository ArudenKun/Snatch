using Snatch.ViewModels;

namespace Snatch.Models.EventData;

public sealed record ShowPageEventData(Type ViewModelType)
{
    public static readonly ShowPageEventData Main = new(typeof(MainViewModel));
    // public static readonly ShowPageEventData Dashboard = new(typeof(DashboardPageViewModel));
    // public static readonly ShowPageEventData Settings = new(typeof(SettingsPageViewModel));
}
