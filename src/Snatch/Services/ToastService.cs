using AutoInterfaceAttributes;
using Avalonia.Controls.Notifications;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;
using ShadUI;
using Snatch.Models;
using Snatch.Options;
using Volo.Abp.DependencyInjection;

namespace Snatch.Services;

[AutoInterface]
[UsedImplicitly]
public sealed class ToastService : IToastService, ISingletonDependency
{
    private readonly ToastManager _manager;
    private readonly AppearanceOptions _options;

    public ToastService(ToastManager manager, IOptions<AppearanceOptions> options)
    {
        _manager = manager;
        _options = options.Value;
    }

    /// <summary>
    /// Creates a toast notification with the specified title, content, and buttons.
    /// </summary>
    /// <param name="title"></param>
    /// <param name="content"></param>
    /// <param name="autoDismiss"></param>
    /// <param name="actionButton"></param>
    /// <returns></returns>
    public ToastBuilder CreateToast(
        string? title,
        string content,
        bool autoDismiss,
        ToastActionButton? actionButton = null
    )
    {
        ToastBuilder toast;
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrEmpty(title))
        {
            toast = _manager.CreateToast(content);
        }
        else
        {
            toast = _manager.CreateToast(title).WithContent(content);
        }

        if (autoDismiss)
        {
            toast.DismissOnClick();
        }

        toast.WithDelay(_options.ToastDuration.Seconds);
        if (actionButton is not null)
        {
            toast.WithAction(actionButton.Label, actionButton.OnClicked);
        }

        return toast;
    }

    public ToastBuilder CreateToast(
        string? title,
        string content,
        ToastActionButton? actionButton = null
    ) => CreateToast(title, content, true, actionButton);

    public void ShowToast(
        string title,
        string content,
        bool autoDismiss = true,
        ToastActionButton? actionButton = null
    ) => CreateToast(title, content, autoDismiss, actionButton).Show();

    public void ShowToast(string title, string content, ToastActionButton? actionButton = null) =>
        CreateToast(title, content, true, actionButton).Show();

    public void ShowToast(
        NotificationType type,
        string? title,
        string content,
        bool autoDismiss,
        ToastActionButton? actionButton = null
    )
    {
        var toast = CreateToast(title, content, autoDismiss, actionButton);
        switch (type)
        {
            case NotificationType.Information:
                toast.ShowInfo();
                break;
            case NotificationType.Error:
                toast.ShowError();
                break;
            case NotificationType.Warning:
                toast.ShowWarning();
                break;
            case NotificationType.Success:
                toast.ShowSuccess();
                break;
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(type),
                    type,
                    "Not valid notification type"
                );
        }
    }

    public void ShowToast(
        NotificationType type,
        string? title,
        string content,
        ToastActionButton? actionButton = null
    ) => ShowToast(type, title, content, true, actionButton);

    public void ShowExceptionToast(
        string? title,
        string content,
        bool autoDismiss = true,
        ToastActionButton? actionButton = null
    ) => ShowToast(NotificationType.Error, title, content, autoDismiss, actionButton);

    public void ShowExceptionToast(
        string? title,
        string content,
        ToastActionButton? actionButton = null
    ) => ShowExceptionToast(title, content, true, actionButton);

    public void ShowExceptionToast(
        Exception ex,
        string? title = null,
        string? content = null,
        bool autoDismiss = true,
        ToastActionButton? actionButton = null
    ) =>
        ShowToast(
            NotificationType.Error,
            title,
            string.IsNullOrWhiteSpace(content)
                ? ex.Message
                : $"{content}{Environment.NewLine}{ex.Message}",
            autoDismiss,
            actionButton
        );
}
