using System.Globalization;

namespace VenueOps.Converters;

public sealed class EventStatusBackgroundConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var status = (value as string ?? string.Empty).ToLowerInvariant();
        return status switch
        {
            "confirmed" => EventStatusPalette.ConfirmedBg,
            "draft"     => EventStatusPalette.DraftBg,
            "completed" => EventStatusPalette.CompletedBg,
            "cancelled" => EventStatusPalette.CancelledBg,
            _           => EventStatusPalette.FallbackBg,
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
