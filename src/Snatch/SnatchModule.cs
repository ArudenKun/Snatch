using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ServiceScan.SourceGenerator;
using Snatch.Services;
using Snatch.ViewModels;
using Snatch.Views;
using SukiUI.Dialogs;
using SukiUI.Toasts;
using Volo.Abp;
using Volo.Abp.Autofac;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Modularity;
using ZLinq;

namespace Snatch;

[DependsOn(typeof(AbpAutofacModule))]
public sealed partial class SnatchModule : AbpModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);
        // context.Services.AddConventionalRegistrar(new ViewConventionalRegistrar());
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddSingleton<ISukiToastManager, SukiToastManager>();
        context.Services.AddSingleton<ISukiDialogManager, SukiDialogManager>();

        AddViews(context.Services);
        AddViewModels(context.Services);
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

    [GenerateServiceRegistrations(
        AssignableTo = typeof(IView<>),
        CustomHandler = nameof(AddViewsHandler)
    )]
    private partial void AddViews(IServiceCollection services);

    private static void AddViewsHandler<TView, TViewModel>(IServiceCollection services)
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
    private partial void AddViewModels(IServiceCollection services);

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
