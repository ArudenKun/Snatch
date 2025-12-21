// using System.Runtime.InteropServices;
// using Avalonia;
// using Avalonia.Controls;
// using Avalonia.Data;
// using Avalonia.Platform;
// using JetBrains.Annotations;
// using R3;
// using ZLinq;
// using WinFormsControl = System.Windows.Forms.Control;
// using WinFormsPanel = System.Windows.Forms.Panel;
//
// namespace Snatch.Controls.WebView;
//
// [PublicAPI]
// public class NativeWebView : NativeControlHost, IDisposable
// {
//     private const string EmptySource = "about:blank";
//
//     private WebView2? _webView; // the actual WebView2 control
//     private WinFormsControl? _hostPanel; // WinForms panel to host WebView2
//     private HWND _prevParentWndProc = HWND.NULL; // original parent WndProc
//     private bool _isSubclassed; // flag if parent is subclassed
//     private GCHandle? _wndProcHandle; // keeps delegate alive for SetWindowLongPtr
//     private bool _isInitialized;
//     private IDisposable? _webViewSubscriptions;
//     private IDisposable? _coreWebVie2Subscriptions;
//
//     public event EventHandler<CoreWebView2InitializationCompletedEventArgs>? WebViewCreated;
//     public event EventHandler<CoreWebView2NavigationCompletedEventArgs>? NavigationCompleted;
//     public event EventHandler<CoreWebView2NavigationStartingEventArgs>? NavigationStarting;
//
//     // ReSharper disable once InconsistentNaming
//     public event EventHandler<CoreWebView2DOMContentLoadedEventArgs>? DOMContentLoaded;
//
//     public event EventHandler<CoreWebView2WebResourceRequestedEventArgs>? WebResourceRequested;
//     public event EventHandler<CoreWebView2WebResourceResponseReceivedEventArgs>? WebResourceResponseReceived;
//
//     public HWND Handle { get; private set; }
//
//     public static readonly StyledProperty<string?> UserDataFolderProperty =
//         AvaloniaProperty.Register<NativeWebView, string?>(
//             nameof(UserDataFolder),
//             defaultBindingMode: BindingMode.OneTime
//         );
//
//     public string? UserDataFolder
//     {
//         get => GetValue(UserDataFolderProperty);
//         set => SetValue(UserDataFolderProperty, value);
//     }
//
//     public CoreWebView2 CoreWebView2 =>
//         _webView?.CoreWebView2
//         ?? throw new InvalidOperationException("CoreWebView2 is not initialized.");
//
//     public CoreWebView2CookieManager CookieManager => CoreWebView2.CookieManager;
//
//     public CoreWebView2Settings Settings => CoreWebView2.Settings;
//
//     public static readonly StyledProperty<Uri?> SourceProperty = AvaloniaProperty.Register<
//         NativeWebView,
//         Uri?
//     >(nameof(Source));
//
//     public Uri? Source
//     {
//         get => GetValue(SourceProperty);
//         set => SetValue(SourceProperty, value);
//     }
//
//     public bool CanGoBack => CoreWebView2.CanGoBack;
//
//     public bool CanGoForward => CoreWebView2.CanGoForward;
//
//     public bool GoBack()
//     {
//         _webView?.GoBack();
//         return true;
//     }
//
//     public bool GoForward()
//     {
//         _webView?.GoForward();
//         return true;
//     }
//
//     public Task<string?> InvokeScript(string scriptName)
//     {
//         return _webView?.ExecuteScriptAsync(scriptName) ?? Task.FromResult<string?>(null);
//     }
//
//     public void Navigate(Uri url)
//     {
//         _webView?.Source = url;
//     }
//
//     public void Navigate(Uri url, HttpMethod httpMethod, IDictionary<string, string> headers)
//     {
//         var headerString = headers
//             .AsValueEnumerable()
//             .Select(x => $"{x.Key}: {x.Value}")
//             .JoinToString('\n');
//
//         Navigate(url, httpMethod, headerString);
//     }
//
//     public void Navigate(Uri url, HttpMethod httpMethod, string headers)
//     {
//         if (
//             _webView is null
//             || !_isInitialized
//             || httpMethod != HttpMethod.Get
//             || httpMethod != HttpMethod.Post
//         )
//             return;
//
//         var request = CoreWebView2.Environment.CreateWebResourceRequest(
//             url.ToString(),
//             httpMethod.ToString().ToUpperInvariant(),
//             null,
//             headers
//         );
//         _webView.CoreWebView2.NavigateWithWebResourceRequest(request);
//     }
//
//     public void NavigateToString(string text)
//     {
//         _webView?.NavigateToString(text);
//     }
//
//     public bool Refresh()
//     {
//         _webView?.Reload();
//         return true;
//     }
//
//     public bool Stop()
//     {
//         _webView?.Stop();
//         return true;
//     }
//
//     protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
//     {
//         base.OnPropertyChanged(change);
//
//         // Guard: Do not run if CoreWebView2 is not yet ready.
//         // The value is already stored in the Avalonia Property system
//         if (!_isInitialized || _webView == null)
//             return;
//
//         if (change.Property == SourceProperty)
//         {
//             var newUrl = change.GetNewValue<Uri?>();
//             _webView?.Source = newUrl;
//         }
//     }
//
//     protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
//     {
//         Handle = parent.Handle;
//
//         // create non-top-level host panel and fill parent
//         _hostPanel = new WinFormsPanel { Dock = DockStyle.Fill };
//         _hostPanel.CreateControl();
//
//         // parent it to Avalonia HWND
//         SetParent(_hostPanel.Handle, Handle);
//
//         var style = GetWindowLongPtr(_hostPanel.Handle, WindowLongFlags.GWL_STYLE);
//         style |= (IntPtr)(WindowStyles.WS_CHILD | WindowStyles.WS_VISIBLE);
//         SetWindowLong(_hostPanel.Handle, WindowLongFlags.GWL_STYLE, style);
//
//         // match panel size to parent
//         ResizeHostToParent();
//
//         // subclass parent to track WM_SIZE for automatic resize
//         SubclassParentWindow();
//
//         // create WebView2 inside host panel
//         _webView = new WebView2 { Dock = DockStyle.Fill };
//         _hostPanel.Controls.Add(_webView);
//         _webView.CreateControl();
//
//         InitializeWebViewAsync().WaitOnDispatcherFrame();
//
//         // return WebView2 HWND to Avalonia
//         return new PlatformHandle(_webView.Handle, "HWND");
//     }
//
//     protected override void DestroyNativeControlCore(IPlatformHandle control)
//     {
//         UnsubclassParentWindow(); // restore parent WndProc
//
//         try
//         {
//             _webView?.Dispose();
//             _hostPanel?.Dispose();
//         }
//         catch
//         {
//             // ignore exceptions during cleanup
//         }
//
//         _webView = null;
//         _hostPanel = null;
//         base.DestroyNativeControlCore(control);
//     }
//
//     private async Task InitializeWebViewAsync()
//     {
//         _webView!.CreationProperties = new CoreWebView2CreationProperties
//         {
//             UserDataFolder = UserDataFolder,
//         };
//
//         _webViewSubscriptions = AddWebViewHandlers();
//         await _webView.EnsureCoreWebView2Async();
//         _coreWebVie2Subscriptions = AddCoreWebViewHandlers();
//
//         _isInitialized = true;
//         _webView.Source = Source ?? new Uri(EmptySource);
//     }
//
//     private IDisposable AddWebViewHandlers() =>
//         Disposable.Combine(
//             _webView
//                 .Events()
//                 .CoreWebView2InitializationCompleted.Subscribe(x =>
//                     WebViewCreated?.Invoke(this, x)
//                 ),
//             _webView
//                 .Events()
//                 .NavigationStarting.Subscribe(x => NavigationStarting?.Invoke(this, x)),
//             _webView
//                 .Events()
//                 .NavigationCompleted.Subscribe(x => NavigationCompleted?.Invoke(this, x))
//         );
//
//     private IDisposable AddCoreWebViewHandlers() =>
//         Disposable.Combine(
//             _webView!
//                 .CoreWebView2.Events()
//                 .DOMContentLoaded.Subscribe(x => DOMContentLoaded?.Invoke(this, x)),
//             _webView
//                 .CoreWebView2.Events()
//                 .WebResourceRequested.Subscribe(x => WebResourceRequested?.Invoke(this, x)),
//             _webView
//                 .CoreWebView2.Events()
//                 .WebResourceResponseReceived.Subscribe(x =>
//                     WebResourceResponseReceived?.Invoke(this, x)
//                 )
//         );
//
//     public void Dispose()
//     {
//         _webViewSubscriptions?.Dispose();
//         _coreWebVie2Subscriptions?.Dispose();
//         GC.SuppressFinalize(this);
//     }
//
//     #region Parent subclass + resize
//
//     // adjust host panel to fill Avalonia HWND
//     private void ResizeHostToParent()
//     {
//         if (_hostPanel == null || Handle == IntPtr.Zero)
//             return;
//
//         if (GetClientRect(Handle, out var rc))
//         {
//             int w = rc.Right - rc.Left;
//             int h = rc.Bottom - rc.Top;
//             MoveWindow(_hostPanel.Handle, 0, 0, Math.Max(0, w), Math.Max(0, h), true);
//         }
//     }
//
//     // subclass parent HWND to intercept size/move messages
//     private void SubclassParentWindow()
//     {
//         if (_isSubclassed || Handle == IntPtr.Zero)
//             return;
//
//         WindowProc newProc = ParentWndProc;
//         _wndProcHandle = GCHandle.Alloc(newProc); // keep delegate alive
//         var ptr = Marshal.GetFunctionPointerForDelegate(newProc);
//         _prevParentWndProc = SetWindowLong(Handle, WindowLongFlags.GWL_WNDPROC, ptr);
//         _isSubclassed = true;
//     }
//
//     // remove subclass and restore original WndProc
//     private void UnsubclassParentWindow()
//     {
//         if (!_isSubclassed || Handle == IntPtr.Zero)
//             return;
//
//         SetWindowLong(Handle, WindowLongFlags.GWL_WNDPROC, (IntPtr)_prevParentWndProc);
//         _prevParentWndProc = IntPtr.Zero;
//
//         if (_wndProcHandle is { IsAllocated: true })
//             _wndProcHandle.Value.Free();
//
//         _isSubclassed = false;
//     }
//
//     // intercept parent window messages to handle resizing
//     private IntPtr ParentWndProc(HWND hwnd, uint msg, IntPtr wParam, IntPtr lParam)
//     {
//         if (
//             (WindowMessage)msg
//             is WindowMessage.WM_SIZE
//                 or WindowMessage.WM_MOVE
//                 or WindowMessage.WM_WINDOWPOSCHANGED
//         )
//             ResizeHostToParent();
//
//         return CallWindowProc((IntPtr)_prevParentWndProc, hwnd, msg, wParam, lParam);
//     }
//
//     #endregion;
// }
