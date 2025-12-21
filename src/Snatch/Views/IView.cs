using ViewModel = Snatch.ViewModels.ViewModel;

namespace Snatch.Views;

public interface IView;

public interface IView<TViewModel> : IView
    where TViewModel : ViewModel
{
    TViewModel ViewModel { get; }
    TViewModel DataContext { get; set; }
}
