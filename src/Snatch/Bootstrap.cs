using System.Reflection;
using Avalonia;
using Humanizer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;
using ServiceScan.SourceGenerator;
using Snatch.Extensions;
using Snatch.Hosting;
using Snatch.Options;
using Snatch.Services;
using Snatch.Utilities;
using Velopack;

namespace Snatch;

public static partial class Bootstrap
{
    private static ILogger Logger => Log.ForContext("SourceContext", nameof(Snatch));

    extension(IHost host)
    {
        public async Task<int> RunSnatchAsync()
        {
            try
            {
                Logger.Information("Starting Avalonia Host");

                await host.InitializeAsync();
                await host.RunAsync();
                return 0;
            }
            catch (Exception ex)
            {
                Logger.Fatal(ex, "Host terminated unexpectedly!");
                return 1;
            }
            finally
            {
                await Log.CloseAndFlushAsync();
            }
        }
    }

    extension(IHostBuilder hostBuilder)
    {
        public IHostBuilder UseSnatch(Action<AppBuilder>? configure = null)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                .Enrich.WithDemystifiedStackTraces()
                .WriteTo.Async(c =>
                    c.File(
                        AppHelper.LogsDir.CombinePath("log.txt"),
                        outputTemplate: LoggingOptions.Template
                    )
                )
                .WriteTo.Async(c => c.Console(outputTemplate: LoggingOptions.Template))
                .CreateBootstrapLogger();

            VelopackApp.Build().SetLogger(VelopackLogger.Instance).Run();

            return hostBuilder
                .ConfigureLogging()
                .ConfigureConfiguration()
                .ConfigureAvalonia(configure)
                .UseAutofac()
                .UseApplication<SnatchModule>()
                .UseConsoleLifetime();
        }

        private IHostBuilder ConfigureLogging() =>
            hostBuilder
                .ConfigureServices(services =>
                    services.AddSingleton(sp => new ObservableLoggingLevelSwitch(
                        sp.GetRequiredService<IOptions<LoggingOptions>>().Value
                    ))
                )
                .UseSerilog(
                    (_, sp, loggingConfiguration) =>
                        loggingConfiguration
                            .MinimumLevel.ControlledBy(
                                sp.GetRequiredService<ObservableLoggingLevelSwitch>()
                            )
                            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                            .MinimumLevel.Override(
                                "Microsoft.EntityFrameworkCore",
                                LogEventLevel.Warning
                            )
                            .Enrich.FromLogContext()
                            .Enrich.WithDemystifiedStackTraces()
                            .WriteTo.Async(c =>
                                c.File(
                                    AppHelper.LogsDir.CombinePath("log.txt"),
                                    outputTemplate: LoggingOptions.Template,
                                    retainedFileTimeLimit: 30.Days(),
                                    rollingInterval: RollingInterval.Day,
                                    rollOnFileSizeLimit: true,
                                    shared: true
                                )
                            )
                            .WriteTo.Async(c => c.Console(outputTemplate: LoggingOptions.Template))
                );

        private IHostBuilder ConfigureConfiguration() =>
            hostBuilder
                .ConfigureServices((ctx, services) => services.AddOptions(ctx.Configuration))
                .ConfigureHostConfiguration(configHost =>
                    configHost
                        .AddConfiguration(
                            ConfigurationHelper.BuildConfiguration(
                                new AbpConfigurationBuilderOptions { BasePath = AppHelper.DataDir }
                            )
                        )
                        .AddAppSettingsSecretsJson()
                )
                .ConfigureAppConfiguration(
                    (_, configApp) =>
                        configApp
                            .AddConfiguration(
                                ConfigurationHelper.BuildConfiguration(
                                    new AbpConfigurationBuilderOptions
                                    {
                                        BasePath = AppHelper.DataDir,
                                    }
                                )
                            )
                            .AddAppSettingsSecretsJson()
                );

        private IHostBuilder ConfigureAvalonia(Action<AppBuilder>? configure = null) =>
            hostBuilder.UseAvaloniaHosting<App>(appBuilder =>
            {
                configure?.Invoke(appBuilder);
                appBuilder
                    .UseR3(exception => Logger.Fatal(exception, "R3 Unhandled Exception"))
                    .LogToTrace();
            });
    }

    [GenerateServiceRegistrations(
        AttributeFilter = typeof(OptionAttribute),
        CustomHandler = nameof(AddOption)
    )]
    public static partial IServiceCollection AddOptions(
        this IServiceCollection services,
        IConfiguration configuration
    );

    private static void AddOption<T>(IServiceCollection services, IConfiguration configuration)
        where T : class
    {
        var sectionKey = typeof(T).GetCustomAttribute<OptionAttribute>()?.Section;
        var section = sectionKey is null ? configuration : configuration.GetSection(sectionKey);
        services.Configure<T>(section);
    }
}
