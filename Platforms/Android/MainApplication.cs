using Android.App;
using Android.Runtime;

namespace LocationTrackerApp;

/// <summary>Android process entry point that delegates shared setup to MauiProgram.</summary>
[Application]
public class MainApplication : MauiApplication
{
    // Android supplies the native handle and its ownership policy during process creation.
    public MainApplication(IntPtr handle, JniHandleOwnership ownership)
        : base(handle, ownership)
    {
    }

    // Keep service and Maps registration in one shared startup method.
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
