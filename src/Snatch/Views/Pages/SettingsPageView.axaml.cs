using Snatch.ViewModels.Pages;

namespace Snatch.Views.Pages;

public partial class SettingsPageView : UserControl<SettingsPageViewModel>
{
    public SettingsPageView()
    {
        InitializeComponent();
    }

    // public NativeWebView NativeWebView { get; }
    //
    // protected override void OnLoaded(RoutedEventArgs e)
    // {
    //     base.OnLoaded(e);
    //
    //     NativeWebView
    //         .Events()
    //         .WebViewCreated.Subscribe(_ =>
    //         {
    //             NativeWebView.CoreWebView2.AddWebResourceRequestedFilter(
    //                 "https://www.youtube.com/embed/*",
    //                 CoreWebView2WebResourceContext.All
    //             );
    //
    //             NativeWebView.Settings.AreDefaultContextMenusEnabled = false;
    //             NativeWebView.Settings.AreDevToolsEnabled = false;
    //             NativeWebView.Settings.IsGeneralAutofillEnabled = false;
    //             NativeWebView.Settings.IsPasswordAutosaveEnabled = false;
    //             NativeWebView.Settings.IsStatusBarEnabled = false;
    //             NativeWebView.Settings.IsSwipeNavigationEnabled = false;
    //         })
    //         .AddTo(Disposables);
    //     NativeWebView
    //         .Events()
    //         .WebResourceRequested.Subscribe(x =>
    //         {
    //             x.Request.Headers.SetHeader(
    //                 "Referer",
    //                 "https://github.com/Tyrrrz/YoutubeDownloader"
    //             );
    //         })
    //         .AddTo(Disposables);
    // }
}
