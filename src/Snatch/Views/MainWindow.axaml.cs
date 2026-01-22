using CommunityToolkit.Mvvm.Messaging;
using Snatch.Messaging.Messages;
using Snatch.ViewModels;

namespace Snatch.Views;

public sealed partial class MainWindow
    : SukiWindow<MainWindowViewModel>,
        IRecipient<ConsoleWindowCloseMessage>,
        IRecipient<ConsoleWindowShowMessage>,
        IRecipient<ConsoleWindowHideMessage>
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

    public void Receive(ConsoleWindowCloseMessage message)
    {
        if (_consoleWindow is null)
        {
            return;
        }

        _consoleWindow.Close();
        _consoleWindow = null;
    }

    public void Receive(ConsoleWindowShowMessage message)
    {
        _consoleWindow ??= _viewLocator.CreateView<ConsoleWindow, ConsoleWindowViewModel>(
            _consoleWindowViewModel
        );
        _consoleWindow.Show();
        Focus();
    }

    public void Receive(ConsoleWindowHideMessage message)
    {
        _consoleWindow?.Hide();
    }
}
