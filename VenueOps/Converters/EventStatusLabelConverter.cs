using System.Globalization;

namespace VenueOps.Converters;

public sealed class EventStatusLabelConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var status = (value as string ?? string.Empty).ToLowerInvariant();
        return status switch
        {
            "confirmed" => "Confirmed",
            "draft"     => "Draft",
            "completed" => "Completed",
            "cancelled" => "Cancelled",
            _           => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(status),
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
