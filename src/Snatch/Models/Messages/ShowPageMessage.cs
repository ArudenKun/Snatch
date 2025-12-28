using Snatch.ViewModels;

namespace Snatch.Models.Messages;

public sealed record ShowPageMessage(Type ViewModelType)
{
    public static readonly ShowPageMessage Main = new(typeof(MainViewModel));
    // public static readonly ShowPageEventData Dashboard = new(typeof(DashboardPageViewModel));
    // public static readonly ShowPageEventData Settings = new(typeof(SettingsPageViewModel));
}
