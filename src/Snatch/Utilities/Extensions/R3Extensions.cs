using Snatch.ViewModels;

namespace Snatch.Utilities.Extensions;

public static class R3Extensions
{
    extension<TDisposable>(TDisposable disposable)
        where TDisposable : IDisposable
    {
        public TDisposable AddTo(ViewModel viewModel)
        {
            viewModel.AddTo(disposable);
            return disposable;
        }
    }
}
