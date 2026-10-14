using SQLite;

namespace LocationTrackerApp;

/// <summary>A recorded position; timestamps are stored in UTC.</summary>
public sealed class LocationPoint
{
    // SQLite assigns a stable identifier; coordinates are decimal degrees.
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    /// <summary>Latitude in decimal degrees, from -90 to 90.</summary>
    public double Latitude { get; set; }
    /// <summary>Longitude in decimal degrees, from -180 to 180.</summary>
    public double Longitude { get; set; }
    /// <summary>UTC observation time used to order saved history.</summary>
    public DateTime Timestamp { get; set; }
    /// <summary>Separates synthetic demonstration data from recorded GPS observations.</summary>
    public bool IsDemo { get; set; }
}

/// <summary>Persists samples in the application's private SQLite database.</summary>
public sealed class DatabaseService
{
    private readonly SQLiteAsyncConnection _database;
    private readonly Task _initialization;

    /// <summary>Opens private app storage and begins nonblocking table initialization.</summary>
    public DatabaseService()
    {
        var path = Path.Combine(FileSystem.AppDataDirectory, "locations.db3");
        _database = new SQLiteAsyncConnection(path);
        // Await this task before every query rather than blocking the UI thread.
        _initialization = _database.CreateTableAsync<LocationPoint>();
    }

    /// <summary>Loads samples in chronological order after ensuring the schema exists.</summary>
    public async Task<List<LocationPoint>> GetLocationsAsync()
    {
        await _initialization;
        return await _database.Table<LocationPoint>().OrderBy(p => p.Timestamp).ToListAsync();
    }

    /// <summary>Validates and inserts one observation using SQLite parameter binding.</summary>
    public async Task SaveLocationAsync(LocationPoint point)
    {
        ArgumentNullException.ThrowIfNull(point);
        // Reject invalid sensor coordinates before they reach storage or the native map.
        if (!double.IsFinite(point.Latitude) || point.Latitude is < -90 or > 90)
        {
            throw new ArgumentOutOfRangeException(nameof(point), "Latitude must be between -90 and 90.");
        }
        if (!double.IsFinite(point.Longitude) || point.Longitude is < -180 or > 180)
        {
            throw new ArgumentOutOfRangeException(nameof(point), "Longitude must be between -180 and 180.");
        }
        await _initialization;
        await _database.InsertAsync(point);
    }

    /// <summary>Atomically replaces only demo rows, leaving GPS records intact.</summary>
    public async Task ReplaceDemoAsync(IEnumerable<LocationPoint> points)
    {
        await _initialization;
        // A transaction avoids leaving half a demo route if an insert fails.
        await _database.RunInTransactionAsync(connection =>
        {
            connection.Execute("DELETE FROM LocationPoint WHERE IsDemo = 1");
            connection.InsertAll(points);
        });
    }

    /// <summary>Removes all rows after the page has obtained user confirmation.</summary>
    public async Task ClearAsync()
    {
        await _initialization;
        await _database.DeleteAllAsync<LocationPoint>();
    }
}
