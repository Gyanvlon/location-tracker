using Foundation;

namespace LocationTrackerApp;

/// <summary>Connects the native Apple app lifecycle to shared MAUI startup.</summary>
[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    // Platform template stub; Android is the configured project target.
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
