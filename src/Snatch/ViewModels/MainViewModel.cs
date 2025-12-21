using CommunityToolkit.Mvvm.ComponentModel;
using Snatch.Models.EventData;
using Snatch.ViewModels.Pages;
using Velopack;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;
using ZLinq;

namespace Snatch.ViewModels;

public sealed partial class MainViewModel
    : ViewModel,
        ILocalEventHandler<ShowPageEventData>,
        ISingletonDependency
{
    private readonly Dictionary<Type, PageViewModel> _pageCache;

    // private bool _updatedChecked;
    private UpdateManager _updateManager;

    public MainViewModel(IEnumerable<IPageViewModel> pageViewModels)
    {
        _pageCache = pageViewModels
            .AsValueEnumerable()
            .OrderBy(x => x.Index)
            .Cast<PageViewModel>()
            .ToDictionary(k => k.GetType(), v => v);

        _updateManager = new UpdateManager("https://github.com/ArudenKun/Snatch");
    }

    public ICollection<PageViewModel> Pages => _pageCache.Values;

    [ObservableProperty]
    public partial PageViewModel Page { get; set; } = null!;

    public override void OnLoaded()
    {
        // if (!_updatedChecked)
        // {
        //     CheckForUpdatesAsync().SafeFireAndForget();
        // }
    }

    public Task HandleEventAsync(ShowPageEventData eventData)
    {
        if (_pageCache.TryGetValue(eventData.ViewModelType, out var cachedPage))
        {
            Page = cachedPage;
        }

        return Task.CompletedTask;
    }

    // private async Task CheckForUpdatesAsync() { }
}
