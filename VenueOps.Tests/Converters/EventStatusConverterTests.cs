using System.Globalization;
using VenueOps.Converters;

namespace VenueOps.Tests.Converters;

public class EventStatusConverterTests
{
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    // ── EventStatusPalette ────────────────────────────────────────────────────

    [Fact]
    public void Palette_DefinesExpectedBrandColors()
    {
        Assert.Equal(Color.FromArgb("#DFF7EC"), EventStatusPalette.ConfirmedBg);
        Assert.Equal(Color.FromArgb("#285A48"), EventStatusPalette.ConfirmedText);
        Assert.Equal(Color.FromArgb("#FFF3E0"), EventStatusPalette.DraftBg);
        Assert.Equal(Color.FromArgb("#E65100"), EventStatusPalette.DraftText);
        Assert.Equal(Color.FromArgb("#E8F0EC"), EventStatusPalette.CompletedBg);
        Assert.Equal(Color.FromArgb("#507870"), EventStatusPalette.CompletedText);
        Assert.Equal(Color.FromArgb("#FDECEA"), EventStatusPalette.CancelledBg);
        Assert.Equal(Color.FromArgb("#C0392B"), EventStatusPalette.CancelledText);
        Assert.Equal(Color.FromArgb("#E8F0EC"), EventStatusPalette.FallbackBg);
        Assert.Equal(Color.FromArgb("#507870"), EventStatusPalette.FallbackText);
    }

    // ── EventStatusBackgroundConverter ────────────────────────────────────────

    public static TheoryData<object?, Color> BackgroundCases => new()
    {
        { "confirmed", EventStatusPalette.ConfirmedBg },
        { "CONFIRMED", EventStatusPalette.ConfirmedBg },
        { "draft", EventStatusPalette.DraftBg },
        { "completed", EventStatusPalette.CompletedBg },
        { "Cancelled", EventStatusPalette.CancelledBg },
        { "on-hold", EventStatusPalette.FallbackBg },
        { null, EventStatusPalette.FallbackBg },
        { 42, EventStatusPalette.FallbackBg }
    };

    [Theory]
    [MemberData(nameof(BackgroundCases))]
    public void BackgroundConverter_MapsStatusToPaletteColor(object? status, Color expected)
    {
        object result = new EventStatusBackgroundConverter().Convert(status, typeof(Color), null, Invariant);

        Assert.Same(expected, result);
    }

    [Fact]
    public void BackgroundConverter_ConvertBack_IsNotSupported()
    {
        Assert.Throws<NotSupportedException>(
            () => new EventStatusBackgroundConverter().ConvertBack(EventStatusPalette.DraftBg, typeof(string), null, Invariant));
    }

    // ── EventStatusTextConverter ──────────────────────────────────────────────

    public static TheoryData<object?, Color> TextCases => new()
    {
        { "confirmed", EventStatusPalette.ConfirmedText },
        { "Draft", EventStatusPalette.DraftText },
        { "COMPLETED", EventStatusPalette.CompletedText },
        { "cancelled", EventStatusPalette.CancelledText },
        { "archived", EventStatusPalette.FallbackText },
        { null, EventStatusPalette.FallbackText },
        { DateTime.MinValue, EventStatusPalette.FallbackText }
    };

    [Theory]
    [MemberData(nameof(TextCases))]
    public void TextConverter_MapsStatusToPaletteColor(object? status, Color expected)
    {
        object result = new EventStatusTextConverter().Convert(status, typeof(Color), null, Invariant);

        Assert.Same(expected, result);
    }

    [Fact]
    public void TextConverter_ConvertBack_IsNotSupported()
    {
        Assert.Throws<NotSupportedException>(
            () => new EventStatusTextConverter().ConvertBack(EventStatusPalette.DraftText, typeof(string), null, Invariant));
    }

    // ── EventStatusLabelConverter ─────────────────────────────────────────────

    [Theory]
    [InlineData("confirmed", "Confirmed")]
    [InlineData("DRAFT", "Draft")]
    [InlineData("Completed", "Completed")]
    [InlineData("cancelled", "Cancelled")]
    [InlineData("on hold", "On Hold")]
    [InlineData("PENDING REVIEW", "Pending Review")]
    [InlineData(null, "")]
    public void LabelConverter_MapsStatusToDisplayLabel(string? status, string expected)
    {
        object result = new EventStatusLabelConverter().Convert(status, typeof(string), null, Invariant);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void LabelConverter_TreatsNonStringValueAsEmpty()
    {
        Assert.Equal(string.Empty, new EventStatusLabelConverter().Convert(7, typeof(string), null, Invariant));
    }

    [Fact]
    public void LabelConverter_ConvertBack_IsNotSupported()
    {
        Assert.Throws<NotSupportedException>(
            () => new EventStatusLabelConverter().ConvertBack("Draft", typeof(string), null, Invariant));
    }
}
