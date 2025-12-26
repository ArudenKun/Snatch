using Snatch.Models.EventData;
using Snatch.ViewModels;

namespace Snatch.Views;

public sealed partial class MainWindow : SukiWindow<MainWindowViewModel>
// ,
// ILocalEventHandler<ConsoleWindowCloseEventData>,
// ILocalEventHandler<ConsoleWindowShowEventData>,
// ILocalEventHandler<ConsoleWindowHideEventData>
{
    private readonly ViewLocator _viewLocator;
    private readonly ConsoleWindowViewModel _consoleWindowViewModel;

    private ConsoleWindow? _consoleWindow;

    public MainWindow(ViewLocator viewLocator, ConsoleWindowViewModel consoleWindowViewModel)
    {
        _viewLocator = viewLocator;
        _consoleWindowViewModel = consoleWindowViewModel;
        InitializeComponent();
    }

    public Task HandleEventAsync(ConsoleWindowCloseEventData eventData)
    {
        if (_consoleWindow is null)
        {
            return Task.CompletedTask;
        }

        _consoleWindow.Close();
        _consoleWindow = null;
        return Task.CompletedTask;
    }

    public Task HandleEventAsync(ConsoleWindowShowEventData eventData)
    {
        _consoleWindow ??= _viewLocator.CreateView<ConsoleWindow, ConsoleWindowViewModel>(
            _consoleWindowViewModel
        );
        _consoleWindow.Show();
        Focus();
        return Task.CompletedTask;
    }

    public Task HandleEventAsync(ConsoleWindowHideEventData eventData)
    {
        _consoleWindow?.Hide();
        return Task.CompletedTask;
    }
}
