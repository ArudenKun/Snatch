using System.Diagnostics.CodeAnalysis;

namespace Snatch.Models;

public class DialogActionButton
{
    public required object ButtonContent { get; init; }
    public Action OnClicked { get; init; } = () => { };
    public bool DismissOnClick { get; init; }
    public string[] Classes { get; init; } = [];

    public DialogActionButton() { }

    [SetsRequiredMembers]
    public DialogActionButton(
        object buttonContent,
        Action? onClicked = null,
        bool dismissOnClick = false,
        string[]? classes = null
    )
    {
        ButtonContent = buttonContent;
        if (onClicked is not null)
        {
            OnClicked = onClicked;
        }

        DismissOnClick = dismissOnClick;
        Classes = classes ?? [];
    }
}
