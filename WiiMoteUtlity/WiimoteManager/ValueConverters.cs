using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace WiimoteManager;

/// <summary>
/// Converts a boolean value to a color (green when true, gray when false).
/// </summary>
public class BoolToColorConverter : IValueConverter
{
    public string OnColor { get; set; } = "#00AA44";
    public string OffColor { get; set; } = "#333333";

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isOn)
        {
            return new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString(isOn ? OnColor : OffColor));
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts connection status to a color (green for connected, red for disconnected).
/// </summary>
public class ConnectionStatusColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isConnected)
        {
            return new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString(
                    isConnected ? "#00AA44" : "#AA3333"));
        }
        return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#666666"));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts rumble intensity to a button color.
/// </summary>
public class RumbleColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is float intensity)
        {
            return new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString(
                    intensity > 0 ? "#FF6600" : "#444444"));
        }
        return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#444444"));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts an empty collection count to Visible/Collapsed visibility.
/// </summary>
public class EmptyCollectionToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int count)
        {
            return count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Inverts a boolean value.
/// </summary>
public class InverseBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return !boolValue;
        }
        return true;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return !boolValue;
        }
        return false;
    }
}

/// <summary>
/// Converts a boolean value to text based on a parameter (e.g. "ON|OFF").
/// </summary>
public class BooleanToTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue && parameter is string options)
        {
            var parts = options.Split('|');
            if (parts.Length == 2)
            {
                return boolValue ? parts[0] : parts[1];
            }
        }
        return value?.ToString() ?? "";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Simple boolean to color converter for Emulation toggle.
/// </summary>
public class BooleanToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
             // Green for ON (#00AA44), Grey for OFF (#444444)
             return new SolidColorBrush(
                 (Color)ColorConverter.ConvertFromString(boolValue ? "#00AA44" : "#444444"));
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts a List<string> to comma-separated string
/// </summary>
public class ListToStringConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is List<string> list)
        {
            return string.Join(", ", list);
        }
        return string.Empty;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string str)
        {
            return str.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                      .ToList();
        }
        return new List<string>();
    }
}

/// <summary>
/// Converts boolean to Visibility
/// </summary>
public class BoolToVisConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
