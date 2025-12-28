using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Messaging;
using Humanizer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;
using ServiceScan.SourceGenerator;
using Snatch.Dependency;
using Snatch.Hosting;
using Snatch.Options;
using Snatch.Services;
using Snatch.Utilities;
using Snatch.Utilities.Extensions;
using Snatch.ViewModels;
using Snatch.Views;
using SukiUI.Dialogs;
using SukiUI.Toasts;
using Velopack;
using ZLinq;

namespace Snatch;

public static partial class SnatchBootstrap
{
    private static ILogger Logger => Log.ForContext("SourceContext", nameof(Snatch));

    extension(IHost host)
    {
        public async Task<int> RunSnatchAsync()
        {
            try
            {
                Logger.Information("Starting Avalonia Host");
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
                .ConfigureServices()
                .ConfigureAvalonia(configure)
                .UseAutofac()
                .UseConsoleLifetime();
        }

        private IHostBuilder ConfigureServices() =>
            hostBuilder.ConfigureServices(
                (_, services) =>
                {
                    services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);
                    services.AddSingleton<ISukiToastManager, SukiToastManager>();
                    services.AddSingleton<ISukiDialogManager, SukiDialogManager>();
                    services.AddServices();
                    services.AddViews();
                    services.AddViewModels();

                    services.AddTransient<TopLevel>(_ => App.TopLevel);
                    services.AddTransient<IClipboard>(sp =>
                        sp.GetRequiredService<TopLevel>().Clipboard!
                    );
                    services.AddTransient<IStorageProvider>(sp =>
                        sp.GetRequiredService<TopLevel>().StorageProvider
                    );
                    services.AddTransient<ILauncher>(sp =>
                        sp.GetRequiredService<TopLevel>().Launcher
                    );

                    services.AddHostedService<LifetimeHostedService>();
                }
            );

        private IHostBuilder ConfigureLogging() =>
            hostBuilder
                .ConfigureServices(services =>
                    services.AddSingleton<ObservableLoggingLevelSwitch>()
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
                // .WriteTo.Async(c =>
                //     c.Sink(sp.GetRequiredService<ListenerLogEventSink>())
                // )
                );

        private IHostBuilder ConfigureConfiguration() =>
            hostBuilder
                .ConfigureServices((ctx, services) => services.AddOptions(ctx.Configuration))
                .ConfigureHostConfiguration(configHost =>
                    configHost.AddConfiguration(
                        ConfigurationHelper.BuildConfiguration(
                            new ConfigurationHelper.ConfigurationBuilderOptions
                            {
                                BasePath = AppHelper.DataDir,
                            }
                        )
                    )
                )
                .ConfigureAppConfiguration(
                    (_, configApp) =>
                        configApp.AddConfiguration(
                            ConfigurationHelper.BuildConfiguration(
                                new ConfigurationHelper.ConfigurationBuilderOptions
                                {
                                    BasePath = AppHelper.DataDir,
                                }
                            )
                        )
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
        AssignableTo = typeof(ISingletonDependency),
        AsSelf = true,
        AsImplementedInterfaces = true,
        Lifetime = ServiceLifetime.Singleton
    )]
    // [GenerateServiceRegistrations(
    //     AssignableTo = typeof(IScopedDependency),
    //     AsSelf = true,
    //     AsImplementedInterfaces = true,
    //     Lifetime = ServiceLifetime.Scoped
    // )]
    // [GenerateServiceRegistrations(
    //     AssignableTo = typeof(ITransientDependency),
    //     AsSelf = true,
    //     AsImplementedInterfaces = true,
    //     Lifetime = ServiceLifetime.Transient
    // )]
    public static partial IServiceCollection AddServices(this IServiceCollection services);

    [GenerateServiceRegistrations(
        AttributeFilter = typeof(OptionAttribute),
        CustomHandler = nameof(AddOptionsHandler)
    )]
    public static partial IServiceCollection AddOptions(
        this IServiceCollection services,
        IConfiguration configuration
    );

    private static void AddOptionsHandler<T>(
        IServiceCollection services,
        IConfiguration configuration
    )
        where T : class, new()
    {
        var sectionKey = typeof(T).GetCustomAttribute<OptionAttribute>()?.Section;
        var section = sectionKey is null ? configuration : configuration.GetSection(sectionKey);
        services
            .Configure<T>(section)
            .AddSingleton(sp => sp.GetRequiredService<IOptions<T>>().Value);
    }

    [GenerateServiceRegistrations(
        AssignableTo = typeof(IView<>),
        CustomHandler = nameof(AddViewsHandler)
    )]
    private static partial void AddViews(this IServiceCollection services);

    private static void AddViewsHandler<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TView,
        TViewModel
    >(IServiceCollection services)
        where TView : Control, IView<TViewModel>
        where TViewModel : ViewModel
    {
        services.AddTransient<TView>();
        services.AddTransient<IView<TViewModel>>(sp => sp.GetRequiredService<TView>());
    }

    [GenerateServiceRegistrations(
        AssignableTo = typeof(ViewModel),
        CustomHandler = nameof(AddViewModelsHandler)
    )]
    private static partial void AddViewModels(this IServiceCollection services);

    private static void AddViewModelsHandler<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TViewModel
    >(IServiceCollection services)
        where TViewModel : ViewModel
    {
        var viewModelType = typeof(TViewModel);
        var lifetime =
            viewModelType.GetCustomAttribute<DependencyAttribute>()?.Lifetime
            ?? ServiceLifetime.Transient;
        var viewModelDescriptor = ServiceDescriptor.Describe(
            viewModelType,
            viewModelType,
            lifetime
        );
        var viewModelBaseDescriptors = EnumerateBaseTypes<ViewModel>(viewModelType)
            .AsValueEnumerable()
            .Select(baseType =>
                ServiceDescriptor.Describe(
                    baseType,
                    sp => sp.GetRequiredService<TViewModel>(),
                    lifetime
                )
            )
            .ToArray();
        services.Add(viewModelDescriptor);
        services.Add(viewModelBaseDescriptors);
    }

    private static IEnumerable<Type> EnumerateBaseTypes<TRoot>(Type t)
    {
        ArgumentNullException.ThrowIfNull(t);

        var baseType = t.BaseType;
        while (baseType is not null && baseType != typeof(object))
        {
            yield return baseType;
            if (baseType == typeof(TRoot))
            {
                yield break;
            }

            baseType = baseType.BaseType;
        }
    }
}
