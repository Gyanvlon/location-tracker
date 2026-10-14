using Microsoft.Maui.Maps;

namespace LocationTrackerApp;

/// <summary>Displays persisted locations and connects page actions to the view model.</summary>
public partial class MainPage : ContentPage
{
    private readonly MainViewModel _viewModel;
    private bool _visible;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    /// <summary>Initializes XAML and supplies the compiled bindings with their view model.</summary>
    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    // MAUI lifecycle overrides are event boundaries, so async void is appropriate here.
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _visible = true;
        _viewModel.LocationsChanged += OnLocationsChanged;
        await RefreshSafelyAsync(true);
    }

    protected override void OnDisappearing()
    {
        _visible = false;
        // Detach the event so a hidden page cannot retain subscriptions or redraw itself.
        _viewModel.LocationsChanged -= OnLocationsChanged;
        _viewModel.StopTracking();
        base.OnDisappearing();
    }

    private async void OnLocationsChanged(object? sender, EventArgs args) => await RefreshSafelyAsync(true);

    /// <summary>Serializes refreshes and handles storage or native-map failures.</summary>
    private async Task RefreshSafelyAsync(bool center)
    {
        await _refreshLock.WaitAsync();
        try
        {
            var points = await _viewModel.GetAllLocationsAsync();
            if (!_visible)
            {
                return;
            }
            // Show the new count before updating the reusable map circles.
            var demoCount = points.Count(point => point.IsDemo);
            SampleCount.Text = $"{points.Count} saved locations ({demoCount} demo)";
            HeatMapRenderer.Render(map, points);
            if (center)
            {
                var last = points.LastOrDefault();
                map.MoveToRegion(MapSpan.FromCenterAndRadius(
                    new Location(last?.Latitude ?? 37.323, last?.Longitude ?? -122.0467),
                    Distance.FromKilometers(0.9)));
            }
        }
        catch (Exception exception)
        {
            _viewModel.Status = $"Could not display saved locations: {exception.Message}";
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    // Button handlers delegate persistence to the view model and keep errors visible.
    private async void OnDemoClicked(object sender, EventArgs args)
    {
        // Prevent overlapping demo transactions from repeated taps.
        DemoButton.IsEnabled = false;
        try
        {
            await _viewModel.LoadDemoAsync();
            await RefreshSafelyAsync(true);
        }
        catch (Exception exception)
        {
            _viewModel.Status = $"Could not load demo: {exception.Message}";
        }
        finally
        {
            DemoButton.IsEnabled = true;
        }
    }

    // Confirm deletion before invoking the irreversible local-history operation.
    private async void OnClearClicked(object sender, EventArgs args)
    {
        if (!await DisplayAlert("Clear history", "Delete all saved GPS and demo samples from this device?", "Delete", "Cancel"))
        {
            return;
        }
        try
        {
            await _viewModel.ClearAsync();
            await RefreshSafelyAsync(false);
        }
        catch (Exception exception)
        {
            _viewModel.Status = $"Could not clear history: {exception.Message}";
        }
    }
}
