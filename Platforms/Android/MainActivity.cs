using Android.App;
using Android.Content.PM;
using Android.OS;

namespace LocationTrackerApp;

/// <summary>Hosts the Android MAUI window and handles configuration changes.</summary>
// SingleTop reuses the activity when launched again; MAUI handles the listed device changes.
[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
}
