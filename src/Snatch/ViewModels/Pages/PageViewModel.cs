using AutoInterfaceAttributes;
using CommunityToolkit.Mvvm.ComponentModel;
using Lucide.Avalonia;

namespace Snatch.ViewModels.Pages;

[AutoInterface(Inheritance = [typeof(IViewModel)])]
public abstract partial class PageViewModel : ViewModel, IPageViewModel
{
    /// <summary>
    /// The index of the page.
    /// </summary>
    public abstract int Index { get; }

    /// <summary>
    /// The display name of the page.
    /// </summary>
    public virtual string DisplayName => GetType().Name.Replace("PageViewModel", string.Empty);

    /// <summary>
    /// The icon of the page.
    /// </summary>
    public abstract LucideIconKind IconKind { get; }

    public Type ViewModelType => GetType();

    /// <summary>
    /// The visibility of the page on the side menu.
    /// </summary>
    [ObservableProperty]
    public partial bool IsVisibleOnSideMenu { get; protected set; } = true;

    /// <summary>
    /// Set to true to auto hide the page on the side menu.
    /// </summary>
    public virtual bool AutoHideOnSideMenu => false;

    public override void OnLoaded()
    {
        if (AutoHideOnSideMenu)
        {
            IsVisibleOnSideMenu = true;
        }

        base.OnLoaded();
    }

    public override void OnUnloaded()
    {
        if (AutoHideOnSideMenu)
        {
            IsVisibleOnSideMenu = false;
        }

        base.OnUnloaded();
    }
}
