using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Snatch.Models.EventData;
using Snatch.ViewModels.Pages;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;
using ZLinq;

namespace Snatch.ViewModels;

[Dependency(ServiceLifetime.Singleton)]
public sealed partial class MainViewModel : ViewModel, ILocalEventHandler<ShowPageEventData>
{
    public MainViewModel(IEnumerable<IPageViewModel> pageViewModels)
    {
        // 1. We create the initial structure based on the injected services.
        // Even if they are transient, we need an initial instance to populate the Menu/Tabs.
        var orderedPages = pageViewModels
            .AsValueEnumerable()
            .OrderBy(x => x.Index)
            .Cast<PageViewModel>()
            .ToArray();

        Pages.AddRange(orderedPages);
    }

    public IAvaloniaList<PageViewModel> Pages { get; } = new AvaloniaList<PageViewModel>();

    [ObservableProperty]
    public partial PageViewModel Page { get; set; }

    public Task HandleEventAsync(ShowPageEventData eventData)
    {
        ChangePage(eventData.ViewModelType);
        return Task.CompletedTask;
    }

    [RelayCommand]
    private void ChangePage(Type viewModelType)
    {
        // 2. Resolve the instance from DI.
        // - If registered as Singleton: Returns the existing instance.
        // - If registered as Transient: Returns a NEW instance (Data Reset).
        var newPage = (PageViewModel)ServiceProvider.GetRequiredService(viewModelType);

        // 3. Handle Cleanup (Crucial for Transient ViewModels)
        // If the old instance in the list is different from the new one, and it's disposable, dispose it.
        var oldPage = Page;
        if (!ReferenceEquals(oldPage, newPage) && oldPage is IDisposable disposableVm)
        {
            disposableVm.Dispose();
        }

        // 4. Update the Collection
        // We replace the item at the specific index. This notifies the UI (Tabs/ListBox)
        // that the content for this slot has changed without rebuilding the whole list.
        Page = newPage;
    }
}
