using System.Globalization;
using Moq;
using VenueOps.Models.Events;
using VenueOps.Services;
using VenueOps.ViewModels;

namespace VenueOps.Tests.ViewModels;

public class EventDetailViewModelTests
{
    private readonly Mock<IEventsService> _eventsMock = new();
    private readonly Mock<INavigationService> _navMock = new();

    public EventDetailViewModelTests()
    {
        _navMock.Setup(n => n.GoBackAsync()).Returns(Task.CompletedTask);
        _navMock.Setup(n => n.PushEditEventModalAsync(It.IsAny<EventDetail>(), It.IsAny<Func<Task>>())).Returns(Task.CompletedTask);
    }

    private EventDetailViewModel CreateSut() => new(_eventsMock.Object, _navMock.Object);

    private static EventDetail CreateDetail() => new()
    {
        Id = "evt-500",
        EventName = "Aurora Night Market",
        VenueName = "Canal Yard",
        Status = "confirmed",
        StartDateUtc = new DateTime(2030, 6, 1, 11, 0, 0, DateTimeKind.Utc),
        EndDateUtc = new DateTime(2030, 6, 1, 11, 30, 0, DateTimeKind.Utc),
        ExpectedAtendees = 1200,
        AttendeeCount = 1180,
        BookingReference = "BK-0500",
        ShortDescription = "Fictional street food and music.",
        Images =
        [
            new EventImage { Url = "https://images.example.invalid/hero.jpg", Credit = "Studio One" },
            new EventImage { Url = "https://images.example.invalid/g1.jpg" },
            new EventImage { Url = "https://images.example.invalid/g2.jpg" }
        ],
        Location = new EventLocation { FormattedAddress = "3 Example Quay" },
        Organizer = new EventOrganizer { Name = "Night Markets Ltd", WebsiteUrl = "https://organizer.example.invalid" },
        Tags = ["food", "music"],
        Links = [new EventLink { Label = "Map", Url = "https://maps.example.invalid/canal-yard" }]
    };

    private EventDetailViewModel CreateLoadedSut(EventDetail detail)
    {
        EventDetailViewModel sut = CreateSut();
        sut.Detail = detail;
        return sut;
    }

    private void SetupDetail(EventDetail? detail) =>
        _eventsMock.Setup(e => e.GetEventDetailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(detail);

    private static void WithInvariantCulture(Action assert)
    {
        CultureInfo original = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        try { assert(); }
        finally { CultureInfo.CurrentCulture = original; }
    }

    // ── Initial state ─────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_SetsTitleAndEmptyComputedState()
    {
        EventDetailViewModel sut = CreateSut();

        Assert.Equal("Event Detail", sut.Title);
        Assert.Null(sut.Detail);
        Assert.False(sut.HasContent);
        Assert.Null(sut.HeroImageUrl);
        Assert.False(sut.HasHeroImage);
        Assert.Null(sut.HeroImageCredit);
        Assert.False(sut.HasImageCredit);
        Assert.False(sut.HasDescription);
        Assert.False(sut.HasLocation);
        Assert.False(sut.HasOrganizer);
        Assert.False(sut.OrganizerHasWebsite);
        Assert.False(sut.HasTags);
        Assert.False(sut.HasLinks);
        Assert.Empty(sut.GalleryImages);
        Assert.False(sut.HasGalleryImages);
        Assert.Equal(string.Empty, sut.DateRangeDisplay);
        Assert.Equal("Exp. Attendees", sut.AttendeesLabel);
        Assert.Equal(string.Empty, sut.AttendeesDisplay);
    }

    [Fact]
    public void IsWideLayout_TogglesCompactLayout()
    {
        EventDetailViewModel sut = CreateSut();

        sut.IsWideLayout = true;

        Assert.False(sut.IsCompactLayout);
    }

    // ── Computed properties from a full detail ────────────────────────────────

    [Fact]
    public void ComputedProperties_ReflectFullDetail()
    {
        EventDetailViewModel sut = CreateLoadedSut(CreateDetail());

        Assert.True(sut.HasContent);
        Assert.Equal("https://images.example.invalid/hero.jpg", sut.HeroImageUrl);
        Assert.True(sut.HasHeroImage);
        Assert.Equal("Studio One", sut.HeroImageCredit);
        Assert.True(sut.HasImageCredit);
        Assert.True(sut.HasDescription);
        Assert.True(sut.HasLocation);
        Assert.True(sut.HasOrganizer);
        Assert.True(sut.OrganizerHasWebsite);
        Assert.True(sut.HasTags);
        Assert.True(sut.HasLinks);
        Assert.Equal(
            ["https://images.example.invalid/g1.jpg", "https://images.example.invalid/g2.jpg"],
            sut.GalleryImages.Select(i => i.Url));
        Assert.True(sut.HasGalleryImages);
    }

    [Fact]
    public void ComputedProperties_AreFalse_WhenOptionalSectionsAreEmpty()
    {
        EventDetail detail = CreateDetail();
        detail.Images = [new EventImage { Url = string.Empty, Credit = string.Empty }];
        detail.ShortDescription = "   ";
        detail.Location = new EventLocation { FormattedAddress = " " };
        detail.Organizer = new EventOrganizer { Name = " ", WebsiteUrl = " " };
        detail.Tags = [];
        detail.Links = [];

        EventDetailViewModel sut = CreateLoadedSut(detail);

        Assert.False(sut.HasHeroImage);
        Assert.False(sut.HasImageCredit);
        Assert.False(sut.HasDescription);
        Assert.False(sut.HasLocation);
        Assert.False(sut.HasOrganizer);
        Assert.False(sut.OrganizerHasWebsite);
        Assert.False(sut.HasTags);
        Assert.False(sut.HasLinks);
        Assert.Empty(sut.GalleryImages); // a single image is the hero, not a gallery
        Assert.False(sut.HasGalleryImages);
    }

    [Fact]
    public void ComputedProperties_HandleMissingSections()
    {
        // Mirrors a payload where the API sends null for optional sections.
        EventDetail detail = CreateDetail();
        detail.Images = [];
        detail.Location = null;
        detail.Organizer = null;
        detail.Tags = null!;
        detail.Links = null!;

        EventDetailViewModel sut = CreateLoadedSut(detail);

        Assert.Null(sut.HeroImageUrl);
        Assert.Null(sut.HeroImageCredit);
        Assert.False(sut.HasLocation);
        Assert.False(sut.HasOrganizer);
        Assert.False(sut.OrganizerHasWebsite);
        Assert.False(sut.HasTags);
        Assert.False(sut.HasLinks);
        Assert.Empty(sut.GalleryImages);
    }

    [Fact]
    public void OrganizerHasWebsite_IsFalse_WhenWebsiteIsNull()
    {
        EventDetail detail = CreateDetail();
        detail.Organizer = new EventOrganizer { Name = "Night Markets Ltd", WebsiteUrl = null };

        EventDetailViewModel sut = CreateLoadedSut(detail);

        Assert.True(sut.HasOrganizer);
        Assert.False(sut.OrganizerHasWebsite);
    }

    [Fact]
    public void Detail_RaisesDependentPropertyNotifications()
    {
        EventDetailViewModel sut = CreateSut();
        List<string?> raised = [];
        sut.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        sut.Detail = CreateDetail();

        Assert.Contains(nameof(EventDetailViewModel.HasContent), raised);
        Assert.Contains(nameof(EventDetailViewModel.HeroImageUrl), raised);
        Assert.Contains(nameof(EventDetailViewModel.DateRangeDisplay), raised);
        Assert.Contains(nameof(EventDetailViewModel.AttendeesDisplay), raised);
        Assert.Contains(nameof(EventDetailViewModel.GalleryImages), raised);
    }

    [Fact]
    public void HasContent_IsFalse_WhileBusyOrAfterLoadError()
    {
        EventDetailViewModel sut = CreateLoadedSut(CreateDetail());
        List<string?> raised = [];
        sut.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        sut.IsBusy = true;
        Assert.False(sut.HasContent);
        Assert.Contains(nameof(EventDetailViewModel.HasContent), raised);

        sut.IsBusy = false;
        sut.IsLoadError = true;
        Assert.False(sut.HasContent);
    }

    // ── Date range ────────────────────────────────────────────────────────────

    [Fact]
    public void DateRangeDisplay_ShowsSingleDateWithTimes_WhenStartAndEndShareALocalDate()
    {
        EventDetail detail = CreateDetail(); // 11:00–11:30 UTC: same local date in every time zone
        DateTime start = detail.StartDateUtc.ToLocalTime();
        DateTime end = detail.EndDateUtc.ToLocalTime();
        EventDetailViewModel sut = CreateLoadedSut(detail);

        WithInvariantCulture(() => Assert.Equal(
            $"{start.ToString("MMM d, yyyy", CultureInfo.InvariantCulture)}  ·  " +
            $"{start.ToString("h:mm tt", CultureInfo.InvariantCulture)} – {end.ToString("h:mm tt", CultureInfo.InvariantCulture)}",
            sut.DateRangeDisplay));
    }

    [Fact]
    public void DateRangeDisplay_ShowsDateSpan_WhenEventCrossesDays()
    {
        EventDetail detail = CreateDetail();
        detail.EndDateUtc = detail.StartDateUtc.AddDays(3);
        DateTime start = detail.StartDateUtc.ToLocalTime();
        DateTime end = detail.EndDateUtc.ToLocalTime();
        EventDetailViewModel sut = CreateLoadedSut(detail);

        WithInvariantCulture(() => Assert.Equal(
            $"{start.ToString("MMM d", CultureInfo.InvariantCulture)} – {end.ToString("MMM d, yyyy", CultureInfo.InvariantCulture)}",
            sut.DateRangeDisplay));
    }

    // ── Duration ──────────────────────────────────────────────────────────────

    private EventDetailViewModel CreateSutWithDuration(TimeSpan duration)
    {
        EventDetail detail = CreateDetail();
        detail.EndDateUtc = detail.StartDateUtc + duration;
        return CreateLoadedSut(detail);
    }

    [Fact]
    public async Task Duration_IsShown_AfterInitializeAsyncLoadsAnEventEndingAfterItStarts()
    {
        SetupDetail(CreateDetail());
        EventDetailViewModel sut = CreateSut();
        sut.ApplyQueryAttributes(new Dictionary<string, object> { ["id"] = "evt-500" });

        await sut.InitializeAsync();

        Assert.True(sut.HasDuration);
        Assert.NotEmpty(sut.DurationDisplay);
    }

    [Fact]
    public void DurationDisplay_ShowsHoursAndMinutes_WhenUnderOneDay()
    {
        EventDetailViewModel sut = CreateSutWithDuration(new TimeSpan(2, 30, 0));

        Assert.Equal("2 h 30 min", sut.DurationDisplay);
        Assert.True(sut.HasDuration);
    }

    [Fact]
    public void DurationDisplay_OmitsMinutes_WhenWholeHours()
    {
        EventDetailViewModel sut = CreateSutWithDuration(TimeSpan.FromHours(3));

        Assert.Equal("3 h", sut.DurationDisplay);
    }

    [Theory]
    [InlineData(23, 59, "23 h 59 min")]
    [InlineData(0, 45, "0 h 45 min")]
    public void DurationDisplay_ShowsEdgesOfTheUnderOneDayFormat(int hours, int minutes, string expected)
    {
        EventDetailViewModel sut = CreateSutWithDuration(new TimeSpan(hours, minutes, 0));

        Assert.Equal(expected, sut.DurationDisplay);
        Assert.True(sut.HasDuration);
    }

    [Fact]
    public void DurationDisplay_ShowsDaysAndHours_WhenOneDayOrMore()
    {
        EventDetailViewModel sut = CreateSutWithDuration(new TimeSpan(2, 4, 0, 0));

        Assert.Equal("2 d 4 h", sut.DurationDisplay);
        Assert.True(sut.HasDuration);
    }

    [Theory]
    [InlineData(24, "1 d")]
    [InlineData(72, "3 d")]
    public void DurationDisplay_OmitsHours_WhenWholeDays(int totalHours, string expected)
    {
        EventDetailViewModel sut = CreateSutWithDuration(TimeSpan.FromHours(totalHours));

        Assert.Equal(expected, sut.DurationDisplay);
    }

    [Fact]
    public void DurationDisplay_TruncatesMinutesBelowTheDisplayedUnit()
    {
        EventDetailViewModel sut = CreateSutWithDuration(new TimeSpan(1, 0, 30, 0));

        Assert.Equal("1 d", sut.DurationDisplay);
    }

    [Fact]
    public void DurationDisplay_TruncatesMinutesBelowHours_WhenOverOneDay()
    {
        EventDetailViewModel sut = CreateSutWithDuration(new TimeSpan(2, 4, 59, 0));

        Assert.Equal("2 d 4 h", sut.DurationDisplay);
    }

    [Fact]
    public void DurationDisplay_IsEmpty_WhenNoDetailIsLoaded()
    {
        EventDetailViewModel sut = CreateSut();

        Assert.Equal(string.Empty, sut.DurationDisplay);
        Assert.False(sut.HasDuration);
    }

    [Fact]
    public void DurationDisplay_IsEmpty_WhenEndEqualsStart()
    {
        EventDetailViewModel sut = CreateSutWithDuration(TimeSpan.Zero);

        Assert.Equal(string.Empty, sut.DurationDisplay);
        Assert.False(sut.HasDuration);
    }

    [Fact]
    public void DurationDisplay_IsEmpty_WhenEndIsBeforeStart()
    {
        EventDetailViewModel sut = CreateSutWithDuration(TimeSpan.FromMinutes(-30));

        Assert.Equal(string.Empty, sut.DurationDisplay);
        Assert.False(sut.HasDuration);
    }

    [Fact]
    public async Task Duration_IsHidden_WhenServiceReturnsNull()
    {
        SetupDetail(null);
        EventDetailViewModel sut = CreateLoadedSut(CreateDetail());
        sut.ApplyQueryAttributes(new Dictionary<string, object> { ["id"] = "evt-missing" });

        await sut.InitializeAsync();

        Assert.Null(sut.Detail);
        Assert.Equal(string.Empty, sut.DurationDisplay);
        Assert.False(sut.HasDuration);
    }

    [Fact]
    public async Task Duration_IsHidden_WhenServiceThrows()
    {
        _eventsMock
            .Setup(e => e.GetEventDetailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("offline"));
        EventDetailViewModel sut = CreateSut();
        sut.ApplyQueryAttributes(new Dictionary<string, object> { ["id"] = "evt-500" });

        await sut.InitializeAsync();

        Assert.Null(sut.Detail);
        Assert.Equal(string.Empty, sut.DurationDisplay);
        Assert.False(sut.HasDuration);
    }

    [Fact]
    public void Detail_RaisesDurationNotifications_AndDurationHidesWhenDetailIsCleared()
    {
        EventDetailViewModel sut = CreateSut();
        List<string?> raised = [];
        sut.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        sut.Detail = CreateDetail();

        Assert.Contains(nameof(EventDetailViewModel.DurationDisplay), raised);
        Assert.Contains(nameof(EventDetailViewModel.HasDuration), raised);
        Assert.True(sut.HasDuration);

        raised.Clear();
        sut.Detail = null;

        Assert.Contains(nameof(EventDetailViewModel.DurationDisplay), raised);
        Assert.Contains(nameof(EventDetailViewModel.HasDuration), raised);
        Assert.Equal(string.Empty, sut.DurationDisplay);
        Assert.False(sut.HasDuration);
    }

    // ── Booking reference ─────────────────────────────────────────────────────

    private EventDetailViewModel CreateSutWithBookingReference(string reference)
    {
        EventDetail detail = CreateDetail();
        detail.BookingReference = reference;
        return CreateLoadedSut(detail);
    }

    [Fact]
    public void BookingReferenceDisplay_ShowsUpperCaseGroupsOfFour_AndHasBookingReferenceIsTrue()
    {
        EventDetailViewModel sut = CreateSutWithBookingReference("vno7k2x9q4");

        Assert.Equal("VNO7-K2X9-Q4", sut.BookingReferenceDisplay);
        Assert.True(sut.HasBookingReference);
    }

    [Theory]
    [InlineData("ab12", "AB12")]
    [InlineData("ab12cd34", "AB12-CD34")]
    [InlineData("ab12c", "AB12-C")]
    [InlineData("a", "A")]
    [InlineData("Vno7K2x9q4", "VNO7-K2X9-Q4")]
    public void BookingReferenceDisplay_GroupsFromTheStartAndUpperCases(string reference, string expected)
    {
        EventDetailViewModel sut = CreateSutWithBookingReference(reference);

        Assert.Equal(expected, sut.BookingReferenceDisplay);
        Assert.True(sut.HasBookingReference);
    }

    [Theory]
    [InlineData("vno7 k2x9-q4")]
    [InlineData(" vno7--k2x9  q4 ")]
    [InlineData("VNO7-K2X9-Q4")]
    public void BookingReferenceDisplay_RemovesSpacesAndHyphensBeforeGrouping(string reference)
    {
        EventDetailViewModel sut = CreateSutWithBookingReference(reference);

        Assert.Equal("VNO7-K2X9-Q4", sut.BookingReferenceDisplay);
        Assert.True(sut.HasBookingReference);
    }

    [Fact]
    public void BookingReferenceDisplay_IsEmpty_WhenNoDetailIsLoaded()
    {
        EventDetailViewModel sut = CreateSut();

        Assert.Equal(string.Empty, sut.BookingReferenceDisplay);
        Assert.False(sut.HasBookingReference);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("-")]
    [InlineData(" - - ")]
    public void BookingReferenceDisplay_IsEmpty_WhenReferenceHasNothingButSeparators(string reference)
    {
        EventDetailViewModel sut = CreateSutWithBookingReference(reference);

        Assert.Equal(string.Empty, sut.BookingReferenceDisplay);
        Assert.False(sut.HasBookingReference);
    }

    [Fact]
    public void BookingReferenceDisplay_IsEmpty_WhenReferenceIsNull()
    {
        // Mirrors a payload where the API sends null for the reference.
        EventDetail detail = CreateDetail();
        detail.BookingReference = null!;
        EventDetailViewModel sut = CreateLoadedSut(detail);

        Assert.Equal(string.Empty, sut.BookingReferenceDisplay);
        Assert.False(sut.HasBookingReference);
    }

    [Fact]
    public void Detail_RaisesBookingReferenceNotifications()
    {
        EventDetailViewModel sut = CreateSut();
        List<string?> raised = [];
        sut.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        sut.Detail = CreateDetail();

        Assert.Contains(nameof(EventDetailViewModel.BookingReferenceDisplay), raised);
        Assert.Contains(nameof(EventDetailViewModel.HasBookingReference), raised);
    }

    [Fact]
    public void BookingReferenceDisplay_FollowsDetail_WhenReplacedOrCleared()
    {
        EventDetailViewModel sut = CreateSutWithBookingReference("vno7k2x9q4");
        List<string?> raised = [];
        sut.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        EventDetail other = CreateDetail();
        other.BookingReference = "ab12 cd34 e";
        sut.Detail = other;

        Assert.Equal("AB12-CD34-E", sut.BookingReferenceDisplay);
        Assert.True(sut.HasBookingReference);
        Assert.Contains(nameof(EventDetailViewModel.BookingReferenceDisplay), raised);

        raised.Clear();
        sut.Detail = null;

        Assert.Equal(string.Empty, sut.BookingReferenceDisplay);
        Assert.False(sut.HasBookingReference);
        Assert.Contains(nameof(EventDetailViewModel.BookingReferenceDisplay), raised);
        Assert.Contains(nameof(EventDetailViewModel.HasBookingReference), raised);
    }

    [Fact]
    public async Task BookingReference_IsShownFormatted_AfterInitializeAsyncLoadsAnEvent()
    {
        EventDetail detail = CreateDetail();
        detail.BookingReference = "vno7k2x9q4";
        SetupDetail(detail);
        EventDetailViewModel sut = CreateSut();
        sut.ApplyQueryAttributes(new Dictionary<string, object> { ["id"] = "evt-500" });

        await sut.InitializeAsync();

        Assert.Equal("VNO7-K2X9-Q4", sut.BookingReferenceDisplay);
        Assert.True(sut.HasBookingReference);
    }

    [Fact]
    public async Task BookingReference_IsHidden_WhenServiceReturnsNull()
    {
        SetupDetail(null);
        EventDetailViewModel sut = CreateSutWithBookingReference("vno7k2x9q4");
        sut.ApplyQueryAttributes(new Dictionary<string, object> { ["id"] = "evt-missing" });

        await sut.InitializeAsync();

        Assert.Equal(string.Empty, sut.BookingReferenceDisplay);
        Assert.False(sut.HasBookingReference);
    }

    [Fact]
    public async Task BookingReference_IsHidden_WhenServiceThrows()
    {
        _eventsMock
            .Setup(e => e.GetEventDetailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("offline"));
        EventDetailViewModel sut = CreateSutWithBookingReference("vno7k2x9q4");
        sut.ApplyQueryAttributes(new Dictionary<string, object> { ["id"] = "evt-500" });

        await sut.InitializeAsync();

        Assert.Equal(string.Empty, sut.BookingReferenceDisplay);
        Assert.False(sut.HasBookingReference);
    }

    // ── Gallery heading ───────────────────────────────────────────────────────

    private static EventDetail CreateDetailWithImages(int imageCount)
    {
        EventDetail detail = CreateDetail();
        detail.Images = Enumerable.Range(0, imageCount)
            .Select(i => new EventImage { Url = $"https://images.example.invalid/img{i}.jpg" })
            .ToList();
        return detail;
    }

    [Fact]
    public void GalleryHeadingText_ShowsTheGalleryCount_NotCountingTheHeroImage()
    {
        EventDetailViewModel sut = CreateLoadedSut(CreateDetailWithImages(4));

        Assert.Equal("Gallery (3)", sut.GalleryHeadingText);
        Assert.Equal(3, sut.GalleryImages.Count);
        Assert.True(sut.HasGalleryImages);
    }

    [Fact]
    public void GalleryHeadingText_ShowsOne_ForAGalleryOfOnePhoto()
    {
        EventDetailViewModel sut = CreateLoadedSut(CreateDetailWithImages(2));

        Assert.Equal("Gallery (1)", sut.GalleryHeadingText);
        Assert.True(sut.HasGalleryImages);
    }

    [Fact]
    public void GalleryHeadingText_IsEmptyAndHidden_WhenNoEventIsLoaded()
    {
        EventDetailViewModel sut = CreateSut();

        Assert.Equal(string.Empty, sut.GalleryHeadingText);
        Assert.False(sut.HasGalleryImages);
    }

    [Fact]
    public void GalleryHeadingText_IsEmptyAndHidden_WhenTheEventHasOnlyTheHeroImage()
    {
        EventDetailViewModel sut = CreateLoadedSut(CreateDetailWithImages(1));

        Assert.Equal(string.Empty, sut.GalleryHeadingText);
        Assert.False(sut.HasGalleryImages);
    }

    [Fact]
    public void GalleryHeadingText_IsEmptyAndHidden_WhenTheEventHasNoImages()
    {
        EventDetailViewModel sut = CreateLoadedSut(CreateDetailWithImages(0));

        Assert.Equal(string.Empty, sut.GalleryHeadingText);
        Assert.False(sut.HasGalleryImages);
    }

    [Fact]
    public void Detail_RaisesGalleryHeadingTextNotification_AndTheValueFollowsTheNewEvent()
    {
        EventDetailViewModel sut = CreateLoadedSut(CreateDetailWithImages(2));
        List<string?> raised = [];
        sut.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        sut.Detail = CreateDetailWithImages(5);

        Assert.Contains(nameof(EventDetailViewModel.GalleryHeadingText), raised);
        Assert.Equal("Gallery (4)", sut.GalleryHeadingText);

        raised.Clear();
        sut.Detail = null;

        Assert.Contains(nameof(EventDetailViewModel.GalleryHeadingText), raised);
        Assert.Equal(string.Empty, sut.GalleryHeadingText);
    }

    [Fact]
    public async Task GalleryHeadingText_IsRefreshed_WhenInitializeAsyncLoadsAndReloadsAnEvent()
    {
        SetupDetail(CreateDetailWithImages(4));
        EventDetailViewModel sut = CreateSut();
        sut.ApplyQueryAttributes(new Dictionary<string, object> { ["id"] = "evt-500" });

        await sut.InitializeAsync();

        Assert.Equal("Gallery (3)", sut.GalleryHeadingText);

        SetupDetail(CreateDetailWithImages(0));

        await sut.InitializeAsync();

        Assert.Equal(string.Empty, sut.GalleryHeadingText);
        Assert.False(sut.HasGalleryImages);
    }

    [Fact]
    public async Task GalleryHeadingText_IsEmpty_WhenServiceReturnsNull()
    {
        SetupDetail(null);
        EventDetailViewModel sut = CreateLoadedSut(CreateDetailWithImages(4));
        sut.ApplyQueryAttributes(new Dictionary<string, object> { ["id"] = "evt-missing" });

        await sut.InitializeAsync();

        Assert.Equal(string.Empty, sut.GalleryHeadingText);
        Assert.False(sut.HasGalleryImages);
    }

    [Fact]
    public async Task GalleryHeadingText_IsEmpty_WhenServiceThrows()
    {
        _eventsMock
            .Setup(e => e.GetEventDetailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("offline"));
        EventDetailViewModel sut = CreateLoadedSut(CreateDetailWithImages(4));
        sut.ApplyQueryAttributes(new Dictionary<string, object> { ["id"] = "evt-500" });

        await sut.InitializeAsync();

        Assert.Equal(string.Empty, sut.GalleryHeadingText);
        Assert.False(sut.HasGalleryImages);
    }

    // ── Attendees ─────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("confirmed", "Exp. Attendees", "1,200")]
    [InlineData("draft", "Exp. Attendees", "1,200")]
    [InlineData("completed", "Attendees", "1,180")]
    [InlineData("CANCELLED", "Attendees", "1,180")]
    public void Attendees_ShowActualCountOnlyForFinalStatuses(string status, string expectedLabel, string expectedDisplay)
    {
        EventDetail detail = CreateDetail();
        detail.Status = status;
        EventDetailViewModel sut = CreateLoadedSut(detail);

        Assert.Equal(expectedLabel, sut.AttendeesLabel);
        WithInvariantCulture(() => Assert.Equal(expectedDisplay, sut.AttendeesDisplay));
    }

    // ── Query attributes + InitializeAsync ────────────────────────────────────

    [Fact]
    public async Task InitializeAsync_LoadsDetailForUnescapedQueryId()
    {
        EventDetail detail = CreateDetail();
        SetupDetail(detail);
        EventDetailViewModel sut = CreateSut();
        sut.ApplyQueryAttributes(new Dictionary<string, object> { ["id"] = "evt%2F500" });

        await sut.InitializeAsync();

        _eventsMock.Verify(e => e.GetEventDetailAsync("evt/500", It.IsAny<CancellationToken>()), Times.Once);
        Assert.Same(detail, sut.Detail);
        Assert.False(sut.IsLoadError);
        Assert.False(sut.IsBusy);
        Assert.True(sut.HasContent);
    }

    public static TheoryData<Dictionary<string, object>> IgnoredQueryCases => new()
    {
        new Dictionary<string, object>(),
        new Dictionary<string, object> { ["id"] = 500 },
        new Dictionary<string, object> { ["other"] = "evt-500" }
    };

    [Theory]
    [MemberData(nameof(IgnoredQueryCases))]
    public async Task InitializeAsync_DoesNothing_WhenNoStringIdWasProvided(Dictionary<string, object> query)
    {
        EventDetailViewModel sut = CreateSut();
        sut.ApplyQueryAttributes(query);

        await sut.InitializeAsync();

        _eventsMock.Verify(e => e.GetEventDetailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task InitializeAsync_DoesNothing_WhenAlreadyBusy()
    {
        EventDetailViewModel sut = CreateSut();
        sut.ApplyQueryAttributes(new Dictionary<string, object> { ["id"] = "evt-500" });
        sut.IsBusy = true;

        await sut.InitializeAsync();

        _eventsMock.Verify(e => e.GetEventDetailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task InitializeAsync_FlagsLoadError_WhenDetailIsNull()
    {
        SetupDetail(null);
        EventDetailViewModel sut = CreateLoadedSut(CreateDetail());
        sut.ApplyQueryAttributes(new Dictionary<string, object> { ["id"] = "evt-missing" });

        await sut.InitializeAsync();

        Assert.Null(sut.Detail);
        Assert.True(sut.IsLoadError);
        Assert.False(sut.HasContent);
        Assert.False(sut.IsBusy);
    }

    [Fact]
    public async Task InitializeAsync_FlagsLoadError_WhenServiceThrows()
    {
        _eventsMock
            .Setup(e => e.GetEventDetailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("offline"));
        EventDetailViewModel sut = CreateSut();
        sut.ApplyQueryAttributes(new Dictionary<string, object> { ["id"] = "evt-500" });

        await sut.InitializeAsync();

        Assert.True(sut.IsLoadError);
        Assert.False(sut.IsBusy);
    }

    [Fact]
    public async Task RetryCommand_ReloadsDetail_AfterLoadError()
    {
        SetupDetail(null);
        EventDetailViewModel sut = CreateSut();
        sut.ApplyQueryAttributes(new Dictionary<string, object> { ["id"] = "evt-500" });
        await sut.InitializeAsync();
        SetupDetail(CreateDetail());

        await sut.RetryCommand.ExecuteAsync(null);

        Assert.False(sut.IsLoadError);
        Assert.NotNull(sut.Detail);
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GoBackCommand_NavigatesBack()
    {
        await CreateSut().GoBackCommand.ExecuteAsync(null);

        _navMock.Verify(n => n.GoBackAsync(), Times.Once);
    }

    [Fact]
    public async Task EditEventCommand_DoesNothing_WhenNoDetailIsLoaded()
    {
        await CreateSut().EditEventCommand.ExecuteAsync(null);

        _navMock.Verify(n => n.PushEditEventModalAsync(It.IsAny<EventDetail>(), It.IsAny<Func<Task>>()), Times.Never);
    }

    [Fact]
    public async Task EditEventCommand_OpensModalWithDetailAndReloadsOnUpdate()
    {
        EventDetail detail = CreateDetail();
        Func<Task>? onUpdated = null;
        _navMock
            .Setup(n => n.PushEditEventModalAsync(It.IsAny<EventDetail>(), It.IsAny<Func<Task>>()))
            .Callback<EventDetail, Func<Task>>((_, cb) => onUpdated = cb)
            .Returns(Task.CompletedTask);
        SetupDetail(detail);
        EventDetailViewModel sut = CreateSut();
        sut.ApplyQueryAttributes(new Dictionary<string, object> { ["id"] = "evt-500" });
        await sut.InitializeAsync();

        await sut.EditEventCommand.ExecuteAsync(null);
        Assert.NotNull(onUpdated);
        await onUpdated();

        _navMock.Verify(n => n.PushEditEventModalAsync(detail, It.IsAny<Func<Task>>()), Times.Once);
        _eventsMock.Verify(e => e.GetEventDetailAsync("evt-500", It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not a url")]
    [InlineData("/relative/path")]
    public async Task OpenLinkCommand_IgnoresMissingOrNonAbsoluteUrls(string? url)
    {
        // Launcher.Default throws outside a running MAUI app, so completing without an
        // exception proves the guard rejected the value before reaching the launcher.
        Exception? ex = await Record.ExceptionAsync(() => CreateSut().OpenLinkCommand.ExecuteAsync(url));

        Assert.Null(ex);
    }
}
