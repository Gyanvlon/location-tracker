using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace LocationTrackerApp;

/// <summary>Coordinates foreground tracking and persistent location history.</summary>
public partial class MainViewModel : ObservableObject
{
    // Keep timing policy in one place so GPS requests and recording use the same interval.
    private static readonly TimeSpan RecordingInterval = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan InitialFixTimeout = TimeSpan.FromSeconds(8);
    private readonly DatabaseService _database;
    // Serialize saves and deletion; protect the GPS snapshot across UI and worker threads.
    private readonly object _locationLock = new();
    private readonly SemaphoreSlim _sampleLock = new(1, 1);
    private CancellationTokenSource? _trackingCancellation;
    private Task? _trackingTask;
    private Location? _latestLocation;

    /// <summary>Receives the shared persistence service through dependency injection.</summary>
    public MainViewModel(DatabaseService database)
    {
        ArgumentNullException.ThrowIfNull(database);
        _database = database;
    }

    // The MVVM generator supplies observable properties and updates the button label.
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TrackingButtonText))]
    private bool isTracking;

    [ObservableProperty]
    private string status = "Tap Start tracking to record your position.";

    /// <summary>Provides the label for the current tracking state.</summary>
    public string TrackingButtonText => IsTracking ? "Stop tracking" : "Start tracking";

    /// <summary>Notifies the page on the UI thread after a successful database insert.</summary>
    public event EventHandler? LocationsChanged;

    /// <summary>Returns persistent history for the page and heat map.</summary>
    public Task<List<LocationPoint>> GetAllLocationsAsync() => _database.GetLocationsAsync();

    // AsyncRelayCommand prevents overlapping starts while permission is being requested.
    [RelayCommand]
    private async Task ToggleTrackingAsync()
    {
        if (IsTracking)
        {
            StopTracking();
            return;
        }

        try
        {
            // Ask only for foreground access; background recording is intentionally unsupported.
            var permission = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            if (permission != PermissionStatus.Granted)
            {
                Status = "Location permission denied. Enable it in Android settings to track.";
                return;
            }

            // Never reuse a previous tracking session's position before receiving a fix.
            lock (_locationLock)
            {
                _latestLocation = null;
            }
            Geolocation.LocationChanged += OnLocationChanged;
            Geolocation.ListeningFailed += OnListeningFailed;
            IsTracking = await Geolocation.StartListeningForegroundAsync(
                new GeolocationListeningRequest(GeolocationAccuracy.Best, RecordingInterval));
            if (!IsTracking)
            {
                StopTracking();
                Status = "Unable to start GPS. Enable location services and retry.";
                return;
            }

            Status = "Waiting for a GPS fix. Recording every 10 seconds while this screen is open.";
            var cancellation = new CancellationTokenSource();
            _trackingCancellation = cancellation;
            // Keep the task owned by this view model; the loop handles its errors.
            // Return immediately so Stop tracking stays available while GPS is pending.
            _trackingTask = Task.Run(() => RecordLocationsAsync(cancellation));
        }
        catch (Exception exception)
        {
            StopTracking();
            Status = $"Could not start tracking: {exception.Message}";
        }
    }

    /// <summary>Cancels recording and detaches GPS events when stopped or backgrounded.</summary>
    public void StopTracking()
    {
        var cancellation = _trackingCancellation;
        _trackingCancellation = null;
        if (cancellation is not null)
        {
            cancellation.Cancel();
            var task = _trackingTask;
            _trackingTask = null;
            // Dispose only after the worker has finished using its cancellation source.
            if (task is null)
            {
                cancellation.Dispose();
            }
            else
            {
                _ = task.ContinueWith(_ => cancellation.Dispose(), TaskScheduler.Default);
            }
        }
        Geolocation.LocationChanged -= OnLocationChanged;
        Geolocation.ListeningFailed -= OnListeningFailed;
        if (Geolocation.IsListeningForeground)
            Geolocation.StopListeningForeground();
        IsTracking = false;
        Status = "Tracking stopped. Saved locations remain on this device.";
    }

    private void OnLocationChanged(object? sender, GeolocationLocationChangedEventArgs args)
    {
        // GPS delivery and recording have separate cadences. A stationary device
        // still contributes dwell-time samples even if no new fix is delivered.
        UpdateLatestLocation(args.Location);
    }

    /// <summary>Accepts the newest fix without racing the background recording loop.</summary>
    private void UpdateLatestLocation(Location location)
    {
        lock (_locationLock)
        {
            // A slower initial request must not overwrite a newer listener update.
            if (_latestLocation is null || location.Timestamp >= _latestLocation.Timestamp)
            {
                _latestLocation = location;
            }
        }
    }

    /// <summary>Records an initial observation and then awaits cancellable clock ticks.</summary>
    private async Task RecordLocationsAsync(CancellationTokenSource cancellation)
    {
        var token = cancellation.Token;
        try
        {
            try
            {
                var first = await Geolocation.GetLocationAsync(
                    new GeolocationRequest(GeolocationAccuracy.Best, InitialFixTimeout), token);
                if (first is not null)
                {
                    UpdateLatestLocation(first);
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                PublishStatus($"Waiting for GPS: {exception.Message}", token);
            }

            // Save once at startup, then on clock ticks rather than GPS events.
            await RecordLatestAsync(token);
            using var timer = new PeriodicTimer(RecordingInterval);
            while (await timer.WaitForNextTickAsync(token))
                await RecordLatestAsync(token);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            // Stopping or backgrounding the app cancels the pending timer tick.
        }
        catch (Exception exception)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                // A failed old session must not stop a newly started session.
                if (!ReferenceEquals(_trackingCancellation, cancellation))
                {
                    return;
                }
                StopTracking();
                Status = $"Tracking failed: {exception.Message}";
            });
        }
    }

    /// <summary>Marshals status updates to the UI and ignores cancelled sessions.</summary>
    private void PublishStatus(string message, CancellationToken token) =>
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (!token.IsCancellationRequested)
            {
                Status = message;
            }
        });

    /// <summary>Saves one timed observation and publishes its count and GPS freshness.</summary>
    private async Task RecordLatestAsync(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        Location? location;
        lock (_locationLock)
        {
            location = _latestLocation;
        }
        if (location is null)
        {
            PublishStatus("Waiting for GPS. No position saved yet; enable location services.", token);
            return;
        }

        await _sampleLock.WaitAsync(token);
        try
        {
            token.ThrowIfCancellationRequested();
            var recordedAt = DateTime.UtcNow;
            await _database.SaveLocationAsync(new LocationPoint
            {
                Latitude = location.Latitude,
                Longitude = location.Longitude,
                // This is the observation time, not the time of the cached GPS fix.
                Timestamp = recordedAt
            });
            if (!token.IsCancellationRequested)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (token.IsCancellationRequested) return;
                    Status = $"Saved at {recordedAt.ToLocalTime():HH:mm:ss}. " +
                        $"Every 10 seconds; latest GPS fix {location.Timestamp.ToLocalTime():HH:mm:ss}.";
                    LocationsChanged?.Invoke(this, EventArgs.Empty);
                });
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            PublishStatus($"Could not save location; retrying in 10 seconds: {exception.Message}", token);
        }
        finally
        {
            _sampleLock.Release();
        }
    }

    // Native GPS errors are delivered as events and must update UI state on the main thread.
    private void OnListeningFailed(object? sender, GeolocationListeningFailedEventArgs args) =>
        MainThread.BeginInvokeOnMainThread(() =>
        {
            StopTracking();
            Status = $"Location service failed: {args.Error}. Tap Start tracking to retry.";
        });

    /// <summary>Replaces synthetic route samples while preserving real GPS history.</summary>
    public async Task LoadDemoAsync()
    {
        // Synthetic Cupertino route: extra dwell samples create visible hot spots.
        var points = new List<LocationPoint>();
        // Historical demo timestamps keep synthetic samples from appearing newer than live GPS.
        var demoStartedAt = DateTime.UtcNow.AddMinutes(-10);
        for (var index = 0; index < 45; index++)
        {
            // A longer busy section produces yellow areas and several red hot spots.
            var repeats = index switch
            {
                >= 8 and <= 34 => 12,
                >= 4 and <= 39 => 8,
                _ => 1
            };
            for (var repeat = 0; repeat < repeats; repeat++)
                points.Add(new LocationPoint
                {
                    Latitude = 37.323 + Math.Sin(index / 7.0) * 0.00065,
                    Longitude = -122.052 + index * 0.00024,
                    Timestamp = demoStartedAt.AddSeconds(points.Count),
                    IsDemo = true
                });
        }
        await _database.ReplaceDemoAsync(points);
        Status = "DEMO route loaded. Synthetic samples are separate from GPS history.";
    }

    /// <summary>Deletes history under the same lock used for timed inserts.</summary>
    public async Task ClearAsync()
    {
        await _sampleLock.WaitAsync();
        try
        {
            await _database.ClearAsync();
        }
        finally
        {
            // Always release the lock, including when SQLite reports an error.
            _sampleLock.Release();
        }
        Status = "Location history cleared.";
    }
}
