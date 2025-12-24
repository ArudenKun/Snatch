using AutoInterfaceAttributes;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using ShadUI;
using Snatch.ViewModels.Dialogs;
using Volo.Abp.DependencyInjection;

namespace Snatch.Services;

[AutoInterface]
[UsedImplicitly]
public sealed class DialogService : IDialogService, ISingletonDependency
{
    private readonly DialogManager _manager;

    public DialogService(DialogManager manager, IServiceProvider serviceProvider)
    {
        _manager = manager;
        ServiceProvider = serviceProvider;
    }

    public IServiceProvider ServiceProvider { get; }

    public SimpleDialogBuilder CreateMessageBox(
        string title,
        string message,
        bool canDismissWithBackgroundClick,
        bool destructive = false
    )
    {
        var builder = _manager.CreateDialog(title, message);

        if (canDismissWithBackgroundClick)
        {
            builder.Dismissible();
        }

        builder.WithPrimaryButton(
            "Ok",
            null,
            destructive ? DialogButtonStyle.Destructive : DialogButtonStyle.Primary
        );

        return builder;
    }

    public void ShowMessageBox(string title, string message, bool canDismissWithBackgroundClick)
    {
        CreateMessageBox(title, message, canDismissWithBackgroundClick).Show();
    }

    public void ShowDialog<TViewModel>(TViewModel viewModel)
        where TViewModel : DialogViewModel => _manager.CreateDialog(viewModel).Show();

    public void ShowDialog<TViewModel>()
        where TViewModel : DialogViewModel =>
        ShowDialog(ServiceProvider.GetRequiredService<TViewModel>());

    public async Task<TResult?> ShowDialogAsync<TResult>(DialogViewModel<TResult> viewModel)
    {
        _manager.CreateDialog(viewModel).Show();
        await viewModel.Completion.Task;
        return viewModel.DialogResult;
    }
}
