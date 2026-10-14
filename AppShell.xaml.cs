namespace LocationTrackerApp;

/// <summary>Hosts the application's navigation structure declared in AppShell.xaml.</summary>
public partial class AppShell : Shell
{
    /// <summary>Loads the shell title, theme and initial map page.</summary>
    public AppShell()
    {
        // MAUI generates this method from the paired XAML file during the build.
        InitializeComponent();
    }
}
