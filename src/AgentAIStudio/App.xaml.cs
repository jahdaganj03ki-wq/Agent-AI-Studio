using Microsoft.UI.Xaml;
using AgentAIStudio.Services;
using AgentAIStudio.Views;

namespace AgentAIStudio;

public partial class App : Application
{
    private Window? _window;
    private static IntPtr _windowHandle;

    public App()
    {
        InitializeComponent();
        UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        LogService.Instance.LogInfo("AppLifecycle", "Application initializing");
    }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        LogService.Instance.LogInfo("AppLifecycle", "Application launched");

        _window = new MainWindow();
        _window.Closed += (s, e) =>
        {
            LogService.Instance.LogInfo("AppLifecycle", "Application closing");
        };
        _window.Activate();
    }

    public Window? MainWindow => _window;

    public static IntPtr GetWindowHandle()
    {
        if (_windowHandle == IntPtr.Zero && Instance is App app && app._window != null)
        {
            _windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(app._window);
        }
        return _windowHandle;
    }

    private static void OnUnhandledException(object? sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        LogService.Instance.LogError("Global", "Unhandled UI exception", e.Exception, new
        {
            message = e.Message
        });
        e.Handled = true;
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        if (e.Exception?.InnerException != null)
        {
            LogService.Instance.LogError("Global", "Unobserved task exception", e.Exception.InnerException);
        }
        else if (e.Exception != null)
        {
            LogService.Instance.LogError("Global", "Unobserved task exception (aggregate)", e.Exception);
        }
        e.SetObserved();
    }
}
