using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Microsoft.Extensions.Logging;
using ServiceScan.SourceGenerator;
using Snatch.Core.Utilities.Extensions;
using Snatch.Dependency;
using Snatch.ViewModels;
using Snatch.Views;

namespace Snatch;

public sealed partial class ViewLocator : IDataTemplate, ISingletonDependency
{
    private static readonly Dictionary<Type, Type> ViewTypeCache = new();

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ViewLocator> _logger;

    public ViewLocator(IServiceProvider serviceProvider, ILogger<ViewLocator> logger)
    {
        RegisterViews();
        _serviceProvider = serviceProvider;
        _logger = logger;
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
        var viewType = ViewTypeCache.GetOrDefault(viewModelType);
        if (viewType is null)
            return CreateText($"Could not find view for {viewModelType.FullName}");

        var view = _serviceProvider.GetService(viewType);
        if (view is not Control control)
            return CreateText($"Could not find view for {viewModelType.FullName}");

        _logger.LogInformation("Created View {View}", viewType.Name);
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

    [GenerateServiceRegistrations(
        AssignableTo = typeof(IView<>),
        CustomHandler = nameof(RegisterViewsHandler)
    )]
    private partial void RegisterViews();

    private static void RegisterViewsHandler<TView, TViewModel>()
        where TView : Control, IView<TViewModel>
        where TViewModel : ViewModel => ViewTypeCache.TryAdd(typeof(TViewModel), typeof(TView));
}
