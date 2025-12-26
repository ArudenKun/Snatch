using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Snatch.Models.EventData;
using Snatch.ViewModels.Pages;
using Volo.Abp.DependencyInjection;
using ZLinq;

namespace Snatch.ViewModels;

[Dependency(ServiceLifetime.Singleton)]
public sealed partial class MainViewModel
    : ViewModel,
        // ILocalEventHandler<ShowPageEventData>
        IRecipient<ShowPageEventData>
{
    private readonly Dictionary<Type, int> _pageIndexMap;

    public MainViewModel(IEnumerable<PageViewModel> pageViewModels)
    {
        // 1. We create the initial structure based on the injected services.
        // Even if they are transient, we need an initial instance to populate the Menu/Tabs.
        var orderedPages = pageViewModels
            .AsValueEnumerable()
            .OrderBy(x => x.Index)
            .Cast<PageViewModel>()
            .ToArray();

        Pages.AddRange(orderedPages);

        // 2. Cache the index of each type for O(1) lookups during navigation
        _pageIndexMap = orderedPages
            .Select((vm, index) => new { Type = vm.GetType(), Index = index })
            .ToDictionary(x => x.Type, x => x.Index);

        Page = Pages[0];
    }

    public IAvaloniaList<PageViewModel> Pages { get; } = new AvaloniaList<PageViewModel>();

    [ObservableProperty]
    public partial PageViewModel Page { get; set; }

    public void Receive(ShowPageEventData message)
    {
        ChangePage(message.ViewModelType);
    }

    private void ChangePage(Type viewModelType)
    {
        // 1. Check if we know this page type
        if (!_pageIndexMap.TryGetValue(viewModelType, out var index))
        {
            return;
        }

        // 2. Resolve the instance from DI.
        // - If registered as Singleton: Returns the existing instance.
        // - If registered as Transient: Returns a NEW instance (Data Reset).
        var newPage = (PageViewModel)ServiceProvider.GetRequiredService(viewModelType);

        // 3. Handle Cleanup (Crucial for Transient ViewModels)
        // If the old instance in the list is different from the new one, and it's disposable, dispose it.
        var oldPage = Pages[index];
        if (!ReferenceEquals(oldPage, newPage) && oldPage is IDisposable disposableVm)
        {
            disposableVm.Dispose();
        }

        // 4. Update the Collection
        // We replace the item at the specific index. This notifies the UI (Tabs/ListBox)
        // that the content for this slot has changed without rebuilding the whole list.
        Page = Pages[index] = newPage;
    }
}
