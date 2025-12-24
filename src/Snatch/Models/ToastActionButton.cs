using System.Diagnostics.CodeAnalysis;

namespace Snatch.Models;

public class ToastActionButton
{
    public required string Label { get; init; }
    public Action OnClicked { get; init; } = () => { };

    public ToastActionButton() { }

    [SetsRequiredMembers]
    public ToastActionButton(string label, Action? onClicked = null)
    {
        Label = label;
        if (onClicked is not null)
        {
            OnClicked = onClicked;
        }
    }
}
