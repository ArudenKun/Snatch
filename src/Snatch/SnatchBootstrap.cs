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
using Microsoft.Extensions.Hosting;
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
using Stashbox.Registration.Fluent;
using SukiUI.Dialogs;
using SukiUI.Toasts;
using Velopack;

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
                .UseStashbox()
                .UseConsoleLifetime();
        }

        private IHostBuilder ConfigureServices() =>
            hostBuilder.ConfigureServices(
                (_, services) =>
                {
                    services.AddSingleton<IMessenger>(StrongReferenceMessenger.Default);
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
                // .ConfigureServices((ctx, services) => services.AddOptions(ctx.Configuration))
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
        ExcludeAssignableTo = typeof(ViewModel),
        CustomHandler = nameof(AddServicesHandler)
    )]
    [GenerateServiceRegistrations(
        AssignableTo = typeof(IScopedDependency),
        ExcludeAssignableTo = typeof(ViewModel),
        CustomHandler = nameof(AddServicesHandler)
    )]
    [GenerateServiceRegistrations(
        AssignableTo = typeof(ITransientDependency),
        ExcludeAssignableTo = typeof(ViewModel),
        CustomHandler = nameof(AddServicesHandler)
    )]
    public static partial IServiceCollection AddServices(this IServiceCollection services);

    private static void AddServicesHandler<T>(IServiceCollection services)
        where T : class
    {
        var type = typeof(T);

        Func<RegistrationConfigurator<T, T>, RegistrationConfigurator<T, T>> func = c =>
            c.AsImplementedTypes()
                .WithInitializer(
                    (instance, _) =>
                    {
                        if (instance is IInitializer initializer)
                        {
                            initializer.OnCreate();
                        }
                    }
                )
                .WithFinalizer(instance =>
                {
                    if (instance is IFinalizer finalizer)
                    {
                        finalizer.OnDestroy();
                    }
                });

        if (type.IsAssignableTo(typeof(ISingletonDependency)))
        {
            services.AddSingleton(func);
        }
        else if (type.IsAssignableTo(typeof(IScopedDependency)))
        {
            services.AddScoped(func);
        }
        else
        {
            services.AddTransient(func);
        }
    }

    // [GenerateServiceRegistrations(
    //     AttributeFilter = typeof(OptionAttribute),
    //     CustomHandler = nameof(AddOptionsHandler)
    // )]
    // public static partial IServiceCollection AddOptions(
    //     this IServiceCollection services,
    //     IConfiguration configuration
    // );
    //
    // private static void AddOptionsHandler<T>(
    //     IServiceCollection services,
    //     IConfiguration configuration
    // )
    //     where T : class, new()
    // {
    //     var sectionKey = typeof(T).GetCustomAttribute<OptionAttribute>()?.Section;
    //     var section = sectionKey is null ? configuration : configuration.GetSection(sectionKey);
    //     services
    //         .Configure<T>(section)
    //         .AddSingleton(sp => sp.GetRequiredService<IOptions<T>>().Value);
    // }

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
        services.AddTransient<TView, TView>(configurator: c =>
            c.AsServiceAlso<IView<TViewModel>>()
                .WithFinalizer(instance =>
                {
                    if (instance is IFinalizer finalizer)
                    {
                        finalizer.OnDestroy();
                    }
                })
        );
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

        Func<
            RegistrationConfigurator<TViewModel, TViewModel>,
            RegistrationConfigurator<TViewModel, TViewModel>
        > func = c => c.AsImplementedTypes()
        // .WithInitializer(
        //     (instance, _) =>
        //     {
        //         if (instance is IInitializer initializer)
        //         {
        //             initializer.OnCreate();
        //         }
        //     }
        // )
        // .WithFinalizer(instance =>
        // {
        //     if (instance is IFinalizer finalizer)
        //     {
        //         finalizer.OnDestroy();
        //     }
        // })
        ;
        switch (lifetime)
        {
            case ServiceLifetime.Singleton:
                services.AddSingleton(func);
                break;
            case ServiceLifetime.Scoped:
                services.AddScoped(func);
                break;
            case ServiceLifetime.Transient:
            default:
                services.AddTransient(func);
                break;
        }
    }
}
