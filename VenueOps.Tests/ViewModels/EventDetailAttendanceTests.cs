using System.ComponentModel;
using System.Globalization;
using Moq;
using VenueOps.Models.Events;
using VenueOps.Services;
using VenueOps.ViewModels;

namespace VenueOps.Tests.ViewModels;

/// <summary>
/// Attendance line of <see cref="EventDetailViewModel"/>: "Attendance: P% of expected".
/// Covers the QA plan cases for AC1 to AC4.
/// </summary>
public class EventDetailAttendanceTests
{
    private const string AttendanceProperty = nameof(EventDetailViewModel.AttendanceDisplay);
    private const string HasAttendanceProperty = nameof(EventDetailViewModel.HasAttendanceDisplay);

    private readonly Mock<IEventsService> _eventsMock = new();
    private readonly Mock<INavigationService> _navMock = new();

    private EventDetailViewModel CreateSut() => new(_eventsMock.Object, _navMock.Object);

    private static EventDetail CreateDetail(int attendees, int expected, string status = "confirmed") => new()
    {
        Id = "evt-600",
        EventName = "Harbour Lights Fair",
        VenueName = "Dock Hall",
        Status = status,
        StartDateUtc = new DateTime(2030, 9, 1, 18, 0, 0, DateTimeKind.Utc),
        EndDateUtc = new DateTime(2030, 9, 1, 22, 0, 0, DateTimeKind.Utc),
        AttendeeCount = attendees,
        ExpectedAtendees = expected,
        BookingReference = "BK-0600"
    };

    private EventDetailViewModel CreateLoadedSut(int attendees, int expected, string status = "confirmed")
    {
        EventDetailViewModel sut = CreateSut();
        sut.Detail = CreateDetail(attendees, expected, status);
        return sut;
    }

    private void SetupDetail(EventDetail? detail) =>
        _eventsMock
            .Setup(e => e.GetEventDetailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(detail);

    private static List<string?> RecordNotifications(EventDetailViewModel sut)
    {
        var raised = new List<string?>();
        sut.PropertyChanged += (_, e) => raised.Add(e.PropertyName);
        return raised;
    }

    private static void WithCulture(CultureInfo culture, Action assert)
    {
        CultureInfo original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = culture;
            assert();
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    // ── AC1: the share, rounded down ──────────────────────────────────────────

    [Fact]
    public void AttendanceDisplay_Reads75Percent_For300Of400()
    {
        EventDetailViewModel sut = CreateLoadedSut(300, 400);

        Assert.Equal("Attendance: 75% of expected", sut.AttendanceDisplay);
        Assert.True(sut.HasAttendanceDisplay);
    }

    [Theory]
    [InlineData(1, 3, "Attendance: 33% of expected")]
    [InlineData(2, 3, "Attendance: 66% of expected")]
    [InlineData(199, 200, "Attendance: 99% of expected")]
    public void AttendanceDisplay_RoundsDown_NotToNearest(int attendees, int expected, string text)
    {
        EventDetailViewModel sut = CreateLoadedSut(attendees, expected);

        Assert.Equal(text, sut.AttendanceDisplay);
        Assert.True(sut.HasAttendanceDisplay);
    }

    [Fact]
    public void AttendanceDisplay_ReadsZeroPercentAndIsVisible_WhenNoOneAttendsAgainstExpected()
    {
        EventDetailViewModel sut = CreateLoadedSut(0, 400);

        Assert.Equal("Attendance: 0% of expected", sut.AttendanceDisplay);
        Assert.True(sut.HasAttendanceDisplay);
    }

    [Theory]
    [InlineData(1, 1, "Attendance: 100% of expected")]
    [InlineData(0, 1, "Attendance: 0% of expected")]
    public void AttendanceDisplay_ReadsAndIsVisible_AtTheSmallestExpectedValue(int attendees, int expected, string text)
    {
        EventDetailViewModel sut = CreateLoadedSut(attendees, expected);

        Assert.Equal(text, sut.AttendanceDisplay);
        Assert.True(sut.HasAttendanceDisplay);
    }

    [Fact]
    public void AttendanceDisplay_DoesNotOverflow_WhenBothCountsAreIntMaxValue()
    {
        EventDetailViewModel sut = CreateLoadedSut(int.MaxValue, int.MaxValue);

        Assert.Equal("Attendance: 100% of expected", sut.AttendanceDisplay);
    }

    [Fact]
    public void AttendanceDisplay_DoesNotDependOnTheDeviceCulture()
    {
        EventDetailViewModel sut = CreateLoadedSut(300, 400);
        var other = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        other.NumberFormat.PercentSymbol = "٪";
        other.NumberFormat.NegativeSign = "−";
        other.NumberFormat.NumberDecimalSeparator = "٫";

        WithCulture(other, () => Assert.Equal("Attendance: 75% of expected", sut.AttendanceDisplay));
    }

    [Theory]
    [InlineData("completed")]
    [InlineData("confirmed")]
    [InlineData("draft")]
    public void AttendanceDisplay_Reads75Percent_InEveryEventStatus(string status)
    {
        EventDetailViewModel sut = CreateLoadedSut(300, 400, status);

        Assert.Equal("Attendance: 75% of expected", sut.AttendanceDisplay);
        Assert.True(sut.HasAttendanceDisplay);
    }

    // ── AC2: no cap above expected ────────────────────────────────────────────

    [Fact]
    public void AttendanceDisplay_ShowsTheRealShare_For480Of400()
    {
        EventDetailViewModel sut = CreateLoadedSut(480, 400);

        Assert.Equal("Attendance: 120% of expected", sut.AttendanceDisplay);
        Assert.True(sut.HasAttendanceDisplay);
    }

    [Theory]
    [InlineData(401, 400, "Attendance: 100% of expected")]
    [InlineData(4000, 400, "Attendance: 1000% of expected")]
    public void AttendanceDisplay_IsNotCapped_JustOverExpectedAndForLargeMultiples(int attendees, int expected, string text)
    {
        EventDetailViewModel sut = CreateLoadedSut(attendees, expected);

        Assert.Equal(text, sut.AttendanceDisplay);
    }

    // ── AC3: empty and hidden ─────────────────────────────────────────────────

    [Fact]
    public void AttendanceDisplay_IsEmptyAndHidden_WhenNoEventIsLoaded()
    {
        EventDetailViewModel sut = CreateSut();

        Assert.Equal(string.Empty, sut.AttendanceDisplay);
        Assert.False(sut.HasAttendanceDisplay);
    }

    [Fact]
    public void AttendanceDisplay_IsEmptyAndHidden_WhenExpectedAttendeesAreZero()
    {
        EventDetailViewModel sut = CreateLoadedSut(10, 0);

        Assert.Equal(string.Empty, sut.AttendanceDisplay);
        Assert.False(sut.HasAttendanceDisplay);
    }

    [Theory]
    [InlineData(-5)]
    [InlineData(-1)]
    public void AttendanceDisplay_IsEmptyAndHidden_WhenExpectedAttendeesAreNegative(int expected)
    {
        EventDetailViewModel sut = CreateLoadedSut(10, expected);

        Assert.Equal(string.Empty, sut.AttendanceDisplay);
        Assert.False(sut.HasAttendanceDisplay);
    }

    [Fact]
    public void AttendanceDisplay_IsEmptyAndHidden_WhenZeroExpectedAndZeroAttendees()
    {
        EventDetailViewModel sut = CreateLoadedSut(0, 0);

        Assert.Equal(string.Empty, sut.AttendanceDisplay);
        Assert.False(sut.HasAttendanceDisplay);
    }

    [Fact]
    public async Task AttendanceDisplay_IsEmptyAndHidden_WhenALaterLoadReturnsNull()
    {
        SetupDetail(CreateDetail(300, 400));
        EventDetailViewModel sut = CreateSut();
        sut.ApplyQueryAttributes(new Dictionary<string, object> { ["id"] = "evt-600" });
        await sut.InitializeAsync();
        Assert.Equal("Attendance: 75% of expected", sut.AttendanceDisplay);

        SetupDetail(null);
        await sut.InitializeAsync();

        Assert.Null(sut.Detail);
        Assert.Equal(string.Empty, sut.AttendanceDisplay);
        Assert.False(sut.HasAttendanceDisplay);
    }

    [Fact]
    public async Task AttendanceDisplay_IsEmptyAndHidden_WhenALaterLoadThrows()
    {
        SetupDetail(CreateDetail(300, 400));
        EventDetailViewModel sut = CreateSut();
        sut.ApplyQueryAttributes(new Dictionary<string, object> { ["id"] = "evt-600" });
        await sut.InitializeAsync();
        Assert.Equal("Attendance: 75% of expected", sut.AttendanceDisplay);

        _eventsMock
            .Setup(e => e.GetEventDetailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("offline"));
        await sut.InitializeAsync();

        Assert.Null(sut.Detail);
        Assert.Equal(string.Empty, sut.AttendanceDisplay);
        Assert.False(sut.HasAttendanceDisplay);
    }

    [Fact]
    public void AttendanceDisplay_IsClearedAndNotifies_WhenDetailIsSetToNull()
    {
        EventDetailViewModel sut = CreateLoadedSut(300, 400);
        List<string?> raised = RecordNotifications(sut);

        sut.Detail = null;

        Assert.Contains(AttendanceProperty, raised);
        Assert.Contains(HasAttendanceProperty, raised);
        Assert.Equal(string.Empty, sut.AttendanceDisplay);
        Assert.False(sut.HasAttendanceDisplay);
    }

    // ── AC4: change notifications ─────────────────────────────────────────────

    [Fact]
    public void Detail_RaisesBothAttendanceProperties_WhenSet()
    {
        EventDetailViewModel sut = CreateSut();
        List<string?> raised = RecordNotifications(sut);

        sut.Detail = CreateDetail(300, 400);

        Assert.Contains(AttendanceProperty, raised);
        Assert.Contains(HasAttendanceProperty, raised);
        Assert.Equal("Attendance: 75% of expected", sut.AttendanceDisplay);
    }

    [Fact]
    public void Detail_RaisesAttendanceProperties_WhenReplacedAndCleared()
    {
        EventDetailViewModel sut = CreateLoadedSut(300, 400);
        List<string?> raised = RecordNotifications(sut);

        sut.Detail = CreateDetail(200, 400);

        Assert.Contains(AttendanceProperty, raised);
        Assert.Contains(HasAttendanceProperty, raised);
        Assert.Equal("Attendance: 50% of expected", sut.AttendanceDisplay);
        Assert.True(sut.HasAttendanceDisplay);

        raised.Clear();
        sut.Detail = null;

        Assert.Contains(AttendanceProperty, raised);
        Assert.Contains(HasAttendanceProperty, raised);
        Assert.Equal(string.Empty, sut.AttendanceDisplay);
        Assert.False(sut.HasAttendanceDisplay);
    }

    [Fact]
    public async Task InitializeAsync_RaisesAttendanceProperties_ForTheClearAndTheLoadedEvent()
    {
        // Load once first: on a fresh view model Detail is already null, so clearing it at the
        // start of the reload would raise nothing and the clear would go untested.
        SetupDetail(CreateDetail(300, 400));
        EventDetailViewModel sut = CreateSut();
        sut.ApplyQueryAttributes(new Dictionary<string, object> { ["id"] = "evt-600" });
        await sut.InitializeAsync();
        List<string?> raised = RecordNotifications(sut);

        SetupDetail(CreateDetail(300, 400));
        await sut.InitializeAsync();

        // Once when Detail is cleared at the start of the load, once when the event arrives.
        Assert.Equal(2, raised.Count(p => p == AttendanceProperty));
        Assert.Equal(2, raised.Count(p => p == HasAttendanceProperty));
        Assert.Equal("Attendance: 75% of expected", sut.AttendanceDisplay);
        Assert.True(sut.HasAttendanceDisplay);
    }

    [Fact]
    public void Detail_HidesAttendanceAndNotifies_WhenReplacedWithExpectedZero()
    {
        EventDetailViewModel sut = CreateLoadedSut(300, 400);
        List<string?> raised = RecordNotifications(sut);

        sut.Detail = CreateDetail(300, 0);

        Assert.False(sut.HasAttendanceDisplay);
        Assert.Equal(string.Empty, sut.AttendanceDisplay);
        Assert.Contains(AttendanceProperty, raised);
        Assert.Contains(HasAttendanceProperty, raised);
    }
}
