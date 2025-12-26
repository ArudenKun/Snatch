using ViewModel = Snatch.ViewModels.ViewModel;

namespace Snatch.Views;

public interface IView<TViewModel>
    where TViewModel : ViewModel
{
    TViewModel ViewModel { get; }
    TViewModel DataContext { get; set; }
}
