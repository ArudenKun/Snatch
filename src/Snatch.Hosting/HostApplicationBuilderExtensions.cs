using System.Diagnostics.CodeAnalysis;
using Autofac;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Snatch.Hosting.Internals;
using Volo.Abp;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace Snatch.Hosting;

public static class HostApplicationBuilderExtensions
{
    public static IHostApplicationBuilder AddAutofac(this IHostApplicationBuilder builder)
    {
        var containerBuilder = new ContainerBuilder();
        builder.Services.AddObjectAccessor(containerBuilder);
        builder.ConfigureContainer(new AbpAutofacServiceProviderFactory(containerBuilder));
        return builder;
    }

    public static async Task<IAbpApplicationWithExternalServiceProvider> AddApplicationAsync<TStartupModule>(
        this HostApplicationBuilder builder,
        Action<AbpApplicationCreationOptions>? optionsAction = null
    )
        where TStartupModule : IAbpModule
    {
        return await builder.Services.AddApplicationAsync<TStartupModule>(options =>
        {
            options.Services.ReplaceConfiguration(builder.Configuration);
            optionsAction?.Invoke(options);
            if (options.Environment.IsNullOrWhiteSpace())
            {
                options.Environment = builder.Environment.EnvironmentName;
            }
        });
    }

    public static async Task<IAbpApplicationWithExternalServiceProvider> AddApplicationAsync(
        this HostApplicationBuilder builder,
        Type startupModuleType,
        Action<AbpApplicationCreationOptions>? optionsAction = null
    )
    {
        return await builder.Services.AddApplicationAsync(
            startupModuleType,
            options =>
            {
                options.Services.ReplaceConfiguration(builder.Configuration);
                optionsAction?.Invoke(options);
                if (options.Environment.IsNullOrWhiteSpace())
                {
                    options.Environment = builder.Environment.EnvironmentName;
                }
            }
        );
    }

    /// <summary>
    /// Adds Avalonia main window to the host's service collection,
    /// and a <see cref="AppBuilder"/> to create the Avalonia application.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <param name="configure">The application builder, also used by the previewer.</param>
    /// <returns>The updated host application builder.</returns>
    public static IHostApplicationBuilder AddAvaloniaHosting<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TApplication
    >(this HostApplicationBuilder builder, Action<IServiceProvider, AppBuilder> configure)
        where TApplication : Application
    {
        builder
            .Services.AddSingleton<TApplication>()
            .AddSingleton<Application>(sp => sp.GetRequiredService<TApplication>())
            .AddSingleton(sp =>
            {
                var appBuilder = AppBuilder.Configure(sp.GetRequiredService<TApplication>);
                configure(sp, appBuilder);
                return appBuilder;
            })
            .AddSingleton<IClassicDesktopStyleApplicationLifetime>(_ =>
                (IClassicDesktopStyleApplicationLifetime?)Application.Current?.ApplicationLifetime
                ?? throw new InvalidOperationException("Avalonia application lifetime is not set.")
            )
            .AddSingleton<AvaloniaThread>()
            .AddHostedService<AvaloniaHostedService>();
        return builder;
    }

    /// <summary>
    /// Adds Avalonia main window to the host's service collection,
    /// and a <see cref="AppBuilder"/> to create the Avalonia application.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <param name="configure">The application builder, also used by the previewer.</param>
    /// <returns>The updated host application builder.</returns>
    public static IHostApplicationBuilder AddAvaloniaHosting<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TApp
    >(this HostApplicationBuilder builder, Action<AppBuilder> configure)
        where TApp : Application =>
        builder.AddAvaloniaHosting<TApp>((_, appBuilder) => configure(appBuilder));
}
