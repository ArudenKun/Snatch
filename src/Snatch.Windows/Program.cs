using Avalonia;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Snatch.Windows;

internal static class Program
{
    private static readonly IHost Host = new HostBuilder()
        .ConfigureDefaults(null)
        .UseSnatch(appBuilder => appBuilder.UsePlatformDetect().LogToTrace())
        .Build();

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static async Task<int> Main() => await Host.RunSnatchAsync();

    // Avalonia configuration, don't remove; also used by visual designer.
    [UsedImplicitly]
    public static AppBuilder BuildAvaloniaApp() => Host.Services.GetRequiredService<AppBuilder>();
}
