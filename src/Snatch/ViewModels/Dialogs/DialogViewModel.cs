using AutoInterfaceAttributes;
using CommunityToolkit.Mvvm.Input;
using SukiUI.Controls;
using SukiUI.Dialogs;

namespace Snatch.ViewModels.Dialogs;

public abstract class DialogViewModel : DialogViewModel<bool>;

[AutoInterface(Inheritance = [typeof(IViewModel)])]
public abstract partial class DialogViewModel<TResult> : ViewModel, IDialogViewModel<TResult>
{
    private bool _isResultSet;

    protected DialogViewModel()
    {
        Dialog = new SukiDialog();
    }

    public TaskCompletionSource<bool> Completion { get; private set; } = new();

    protected ISukiDialog Dialog { get; private set; }

    public TResult? DialogResult { get; private set; }

    /// <summary>
    /// Gets the title of the dialog.
    /// </summary>
    public virtual string DialogTitle => string.Empty;

    public override void OnLoaded()
    {
        Reset();
    }

    public override void OnUnloaded()
    {
        Reset();
    }

    [RelayCommand]
    protected void Close(TResult? result = default)
    {
        DialogResult = result;
        Completion.SetResult(result is not null);
        _isResultSet = true;
        Dialog.Dismiss();
    }

    public void SetDialog(ISukiDialog dialog)
    {
        Dialog = dialog;
    }

    protected void Reset()
    {
        if (!_isResultSet)
            return;

        Completion = new TaskCompletionSource<bool>();
        DialogResult = default;
        _isResultSet = false;
    }
}
