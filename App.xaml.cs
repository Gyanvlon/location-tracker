namespace LocationTrackerApp;

/// <summary>Creates the application window and stops foreground recording when hidden.</summary>
public partial class App : Application
{
    private readonly MainViewModel _viewModel;

    /// <summary>Uses the same tracking view model as the map page.</summary>
    public App(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
    }

    // Window.Stopped also covers leaving the app through Android Home or task switching.
    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new AppShell());
        // This assignment records foreground locations only. Resume explicitly with Start.
        window.Stopped += (_, _) => _viewModel.StopTracking();
        return window;
    }
}
