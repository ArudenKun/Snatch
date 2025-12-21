using Snatch.ViewModels;

namespace Snatch.Extensions;

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
