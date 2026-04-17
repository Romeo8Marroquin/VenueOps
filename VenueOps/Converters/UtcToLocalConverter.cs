using System.Globalization;

namespace VenueOps.Converters;

/// <summary>
/// Converts a UTC <see cref="DateTime"/> to a formatted local-time string.
/// Uses the device's timezone via <see cref="TimeZoneInfo.Local"/>.
/// Configure the output format through the <see cref="Format"/> property.
/// Default: "MMM d, h:mm tt"  →  "Apr 18, 7:00 PM"
/// </summary>
public sealed class UtcToLocalConverter : IValueConverter
{
    public string Format { get; set; } = "MMM d, h:mm tt";

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not DateTime rawDate) return string.Empty;

        // Ensure the DateTime is treated as UTC even if Kind is Unspecified
        var utc   = DateTime.SpecifyKind(rawDate, DateTimeKind.Utc);
        var local = TimeZoneInfo.ConvertTimeFromUtc(utc, TimeZoneInfo.Local);

        var fmt = parameter as string ?? Format;
        return local.ToString(fmt, culture);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
