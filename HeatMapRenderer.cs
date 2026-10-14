using System.Runtime.CompilerServices;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using Map = Microsoft.Maui.Controls.Maps.Map;

namespace LocationTrackerApp;

/// <summary>Extends MAUI Maps with a density overlay made of reusable circles.</summary>
public static class HeatMapRenderer
{
    // Weak keys allow discarded map pages and their cached overlays to be collected.
    private static readonly ConditionalWeakTable<Map, RenderState> States = new();

    /// <summary>Tracks the native circles currently attached to a particular map.</summary>
    private sealed class RenderState
    {
        public Dictionary<(double Latitude, double Longitude), CellVisual> Cells { get; } = new();
    }

    /// <summary>Keeps a cell's last center and color to avoid unnecessary native updates.</summary>
    private sealed class CellVisual
    {
        public required Location Center { get; set; }
        public required Color Color { get; set; }
        public required Circle[] Circles { get; init; }
    }

    /// <summary>Updates the relative-density overlay; call only from the UI thread.</summary>
    public static void Render(Map map, IReadOnlyList<LocationPoint> points)
    {
        var state = States.GetOrCreateValue(map);
        // Bin positions into approximately 25 metre cells. All history stays in
        // SQLite; only the latest 2000 samples contribute to the rendered overlay.
        var cells = points.TakeLast(2000).GroupBy(point => (
            Latitude: Math.Round(point.Latitude / 0.000225),
            Longitude: Math.Round(point.Longitude * Math.Cos(point.Latitude * Math.PI / 180) / 0.000225)))
            .Select(group => new
            {
                Key = group.Key,
                Latitude = group.Average(point => point.Latitude),
                Longitude = group.Average(point => point.Longitude),
                Count = group.Count()
            }).ToList();
        // Remove cells that were cleared or aged out of the rendering window.
        var activeKeys = cells.Select(cell => cell.Key).ToHashSet();
        foreach (var key in state.Cells.Keys.Where(key => !activeKeys.Contains(key)).ToList())
        {
            foreach (var circle in state.Cells[key].Circles)
                map.MapElements.Remove(circle);
            state.Cells.Remove(key);
        }

        // Normalize against the busiest cell; drawing low density first exposes hot spots.
        var maximum = cells.Count == 0 ? 1 : cells.Max(cell => cell.Count);
        foreach (var cell in cells.OrderBy(cell => cell.Count))
        {
            // Square-root scaling reveals moderate activity while preserving density order.
            var intensity = maximum == 1 ? 0 : Math.Sqrt((cell.Count - 1.0) / (maximum - 1));
            var color = intensity < 0.33 ? Colors.Blue : intensity < 0.66 ? Colors.Cyan :
                intensity < 0.85 ? Colors.Gold : Colors.Red;
            var center = new Location(cell.Latitude, cell.Longitude);
            if (!state.Cells.TryGetValue(cell.Key, out var visual))
            {
                // Outer translucent rings soften the edge of the stronger center circle.
                var circles = new List<Circle>();
                for (var layer = 3; layer >= 1; layer--)
                {
                    var circle = new Circle
                    {
                        Center = center,
                        Radius = Distance.FromMeters(12 + layer * 9),
                        FillColor = color.WithAlpha(layer == 1 ? 0.65f : 0.16f),
                        StrokeColor = Colors.Transparent,
                        StrokeWidth = 0
                    };
                    circles.Add(circle);
                    map.MapElements.Add(circle);
                }
                state.Cells[cell.Key] = new CellVisual
                {
                    Center = center,
                    Color = color,
                    Circles = circles.ToArray()
                };
                continue;
            }

            // Updating a stationary sample usually requires no native map changes.
            // Recreating every circle on each save can block the Android UI for seconds.
            if (Math.Abs(visual.Center.Latitude - center.Latitude) > 0.00000001 ||
                Math.Abs(visual.Center.Longitude - center.Longitude) > 0.00000001)
            {
                foreach (var circle in visual.Circles)
                    circle.Center = center;
                visual.Center = center;
            }
            if (!visual.Color.Equals(color))
            {
                for (var index = 0; index < visual.Circles.Length; index++)
                    visual.Circles[index].FillColor = color.WithAlpha(index == 2 ? 0.65f : 0.16f);
                visual.Color = color;
            }
        }
    }
}
