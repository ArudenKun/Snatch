using System.Collections.Specialized;
using Avalonia;
using Avalonia.LogicalTree;
using Snatch.Messaging.Messages;
using Snatch.Models;
using Snatch.Utilities;
using Snatch.ViewModels;

namespace Snatch.Views;

public sealed partial class ConsoleWindow : SukiWindow<ConsoleWindowViewModel>
{
    private LogMessage? _item;

    public ConsoleWindow()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        ViewModel.Entries.CollectionChanged += EntriesOnCollectionChanged;
    }

    private void EntriesOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) =>
        _item = DispatchHelper.Invoke(() => ViewModel.Entries.LastOrDefault());

    private void OnLayoutUpdated(object? sender, EventArgs e)
    {
        if (!CanAutoScroll.IsChecked.HasValue || _item is null)
            return;

        if (!CanAutoScroll.IsChecked.Value)
            return;

        DispatchHelper.Invoke(() => LogDataGrid.ScrollIntoView(_item, null));
        _item = null;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        if (!CanAutoScroll.IsChecked.HasValue || _item is null)
            return;

        if (!CanAutoScroll.IsChecked.Value)
            return;

        DispatchHelper.Invoke(() => LogDataGrid.ScrollIntoView(_item, null));
        _item = null;
    }

    protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromLogicalTree(e);

        ViewModel.Entries.CollectionChanged -= EntriesOnCollectionChanged;
    }
}
