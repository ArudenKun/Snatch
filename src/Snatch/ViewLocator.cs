using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Snatch.ViewModels;
using Snatch.Views;
using Volo.Abp.DependencyInjection;

namespace Snatch;

public sealed class ViewLocator : IDataTemplate, ISingletonDependency
{
    private static readonly Dictionary<Type, Type> ViewTypeCache = new();
    private static readonly Type OpenGenericViewType = typeof(IView<>);

    private readonly IServiceProvider _serviceProvider;

    public ViewLocator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public TView CreateView<TView, TViewModel>(TViewModel viewModel)
        where TView : Control
        where TViewModel : ViewModel
    {
        return (TView)CreateView(viewModel);
    }

    public Control CreateView(ViewModel viewModel)
    {
        var viewModelType = viewModel.GetType();
        var viewType = ViewTypeCache.GetOrAdd(
            viewModelType,
            k => OpenGenericViewType.MakeGenericType(k)
        );

        var view = _serviceProvider.GetService(viewType);
        if (view is not Control control)
        {
            return CreateText($"Could not find view for {viewModelType.FullName}");
        }

        control.DataContext = viewModel;
        return control;
    }

    Control ITemplate<object?, Control?>.Build(object? data)
    {
        if (data is ViewModel viewModel)
        {
            return CreateView(viewModel);
        }

        return CreateText($"Could not find view for {data?.GetType().FullName}");
    }

    bool IDataTemplate.Match(object? data) => data is ViewModel;

    private static TextBlock CreateText(string text) => new() { Text = text };
}
