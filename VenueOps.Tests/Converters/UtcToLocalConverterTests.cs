using System.Globalization;
using VenueOps.Converters;

namespace VenueOps.Tests.Converters;

public class UtcToLocalConverterTests
{
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;
    private static readonly DateTime SampleUtc = new(2030, 4, 18, 19, 5, 0, DateTimeKind.Utc);

    private static string ExpectedLocal(DateTime utc, string format) =>
        TimeZoneInfo.ConvertTimeFromUtc(utc, TimeZoneInfo.Local).ToString(format, Invariant);

    [Fact]
    public void Format_HasReadableDefault()
    {
        Assert.Equal("MMM d, h:mm tt", new UtcToLocalConverter().Format);
    }

    [Fact]
    public void Convert_UsesDefaultFormat_WhenNoParameterIsGiven()
    {
        object? result = new UtcToLocalConverter().Convert(SampleUtc, typeof(string), null, Invariant);

        Assert.Equal(ExpectedLocal(SampleUtc, "MMM d, h:mm tt"), result);
    }

    [Fact]
    public void Convert_UsesConfiguredFormatProperty()
    {
        UtcToLocalConverter sut = new() { Format = "yyyy-MM-dd HH:mm" };

        object? result = sut.Convert(SampleUtc, typeof(string), null, Invariant);

        Assert.Equal(ExpectedLocal(SampleUtc, "yyyy-MM-dd HH:mm"), result);
    }

    [Fact]
    public void Convert_PrefersStringParameterOverFormatProperty()
    {
        UtcToLocalConverter sut = new() { Format = "yyyy-MM-dd" };

        object? result = sut.Convert(SampleUtc, typeof(string), "HH:mm", Invariant);

        Assert.Equal(ExpectedLocal(SampleUtc, "HH:mm"), result);
    }

    [Fact]
    public void Convert_TreatsUnspecifiedKindAsUtc()
    {
        DateTime unspecified = DateTime.SpecifyKind(SampleUtc, DateTimeKind.Unspecified);
        UtcToLocalConverter sut = new() { Format = "o" };

        Assert.Equal(
            sut.Convert(SampleUtc, typeof(string), null, Invariant),
            sut.Convert(unspecified, typeof(string), null, Invariant));
    }

    [Fact]
    public void Convert_FormatsWithSuppliedCulture()
    {
        CultureInfo culture = CultureInfo.GetCultureInfo("fr-FR");

        object? result = new UtcToLocalConverter { Format = "MMMM" }.Convert(SampleUtc, typeof(string), null, culture);

        Assert.Equal(TimeZoneInfo.ConvertTimeFromUtc(SampleUtc, TimeZoneInfo.Local).ToString("MMMM", culture), result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("2030-04-18T19:05:00Z")]
    [InlineData(12345)]
    public void Convert_ReturnsEmpty_WhenValueIsNotADateTime(object? value)
    {
        Assert.Equal(string.Empty, new UtcToLocalConverter().Convert(value, typeof(string), null, Invariant));
    }

    [Fact]
    public void ConvertBack_IsNotSupported()
    {
        Assert.Throws<NotSupportedException>(
            () => new UtcToLocalConverter().ConvertBack("Apr 18, 7:05 PM", typeof(DateTime), null, Invariant));
    }
}
