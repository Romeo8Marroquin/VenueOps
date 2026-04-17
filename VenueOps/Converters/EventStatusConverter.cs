using System.Globalization;

namespace VenueOps.Converters;

/// <summary>
/// Converts an event status string to a display value.
/// Pass <c>ConverterParameter</c> to select the output:
///   (none / "background") → badge background <see cref="Color"/>
///   "text"                → badge text <see cref="Color"/>
///   "label"               → human-readable string ("Confirmed", "Draft", …)
/// </summary>
public sealed class EventStatusConverter : IValueConverter
{
    // Colors derived from the VenueOps brand palette —
    // soft tinted backgrounds with on-brand text to avoid harsh contrast.
    private static readonly Color ConfirmedBg   = Color.FromArgb("#DFF7EC");
    private static readonly Color ConfirmedText = Color.FromArgb("#285A48"); // BrandDark
    private static readonly Color DraftBg       = Color.FromArgb("#FFF3E0");
    private static readonly Color DraftText     = Color.FromArgb("#E65100");
    private static readonly Color CompletedBg   = Color.FromArgb("#E8F0EC"); // Gray100
    private static readonly Color CompletedText = Color.FromArgb("#507870"); // Gray500
    private static readonly Color CancelledBg   = Color.FromArgb("#FDECEA");
    private static readonly Color CancelledText = Color.FromArgb("#C0392B"); // Error
    private static readonly Color FallbackBg    = Color.FromArgb("#E8F0EC");
    private static readonly Color FallbackText  = Color.FromArgb("#507870");

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var status = (value as string ?? string.Empty).ToLowerInvariant();
        var mode   = (parameter as string ?? "background").ToLowerInvariant();

        return mode switch
        {
            "label" => status switch
            {
                "confirmed" => "Confirmed",
                "draft"     => "Draft",
                "completed" => "Completed",
                "cancelled" => "Cancelled",
                _           => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(status),
            },
            "text" => (object)(status switch
            {
                "confirmed" => ConfirmedText,
                "draft"     => DraftText,
                "completed" => CompletedText,
                "cancelled" => CancelledText,
                _           => FallbackText,
            }),
            _ => (object)(status switch
            {
                "confirmed" => ConfirmedBg,
                "draft"     => DraftBg,
                "completed" => CompletedBg,
                "cancelled" => CancelledBg,
                _           => FallbackBg,
            }),
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
