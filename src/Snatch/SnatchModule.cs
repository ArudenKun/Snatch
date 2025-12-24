using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.DependencyInjection;
using ShadUI;
using Snatch.Services;
using Snatch.Views;
using Volo.Abp;
using Volo.Abp.Autofac;
using Volo.Abp.EventBus;
using Volo.Abp.Modularity;

namespace Snatch;

[DependsOn(typeof(AbpAutofacModule), typeof(AbpEventBusModule))]
public sealed class SnatchModule : AbpModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddConventionalRegistrar(new ViewConventionalRegistrar());
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddSingleton<ToastManager>();
        context.Services.AddSingleton<DialogManager>();
    }

    public override void PostConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddTransient<TopLevel>(_ => App.TopLevel);
        context.Services.AddTransient<IClipboard>(sp =>
            sp.GetRequiredService<TopLevel>().Clipboard!
        );
        context.Services.AddTransient<IStorageProvider>(sp =>
            sp.GetRequiredService<TopLevel>().StorageProvider
        );
        context.Services.AddTransient<ILauncher>(sp => sp.GetRequiredService<TopLevel>().Launcher);
    }

    public override void OnApplicationShutdown(ApplicationShutdownContext context)
    {
        context.ServiceProvider.GetRequiredService<SettingsService>().Save();
    }
}
