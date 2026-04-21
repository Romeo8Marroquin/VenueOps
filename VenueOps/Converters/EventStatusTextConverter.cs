using System.Globalization;

namespace VenueOps.Converters;

public sealed class EventStatusTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var status = (value as string ?? string.Empty).ToLowerInvariant();
        return status switch
        {
            "confirmed" => EventStatusPalette.ConfirmedText,
            "draft"     => EventStatusPalette.DraftText,
            "completed" => EventStatusPalette.CompletedText,
            "cancelled" => EventStatusPalette.CancelledText,
            _           => EventStatusPalette.FallbackText,
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
