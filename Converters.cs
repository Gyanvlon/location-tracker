using System.Globalization;

namespace LocationTrackerApp;

/// <summary>Converts a tracking flag into a display label for one-way XAML bindings.</summary>
public sealed class BoolToTextConverter : IValueConverter
{
    /// <summary>Returns a safe default when the binding does not supply a Boolean.</summary>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // A converter formats the label; tracking state remains owned by the view model.
        return value is true ? "Stop" : "Start";
    }

    /// <summary>Declines reverse conversion because a label does not control tracking.</summary>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // UnsetValue tells the binding engine there is no reverse value to apply.
        return BindableProperty.UnsetValue;
    }
}
