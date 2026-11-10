using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Moq;
using Newtonsoft.Json.Linq;
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

    private readonly Mock<IClipboardService> _clipboardMock = new();

    private EventDetailViewModel CreateSut() => new(_eventsMock.Object, _navMock.Object, _clipboardMock.Object);

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

    // ── Links heading ─────────────────────────────────────────────────────────

    private static EventDetail CreateDetailWithLinks(int linkCount)
    {
        EventDetail detail = CreateDetail();
        detail.Links = Enumerable.Range(0, linkCount)
            .Select(i => new EventLink { Label = $"Link {i}", Url = $"https://links.example.invalid/{i}" })
            .ToList();
        return detail;
    }

    [Fact]
    public void LinksHeadingText_ShowsTheCount_WhenTheEventHasTwoLinks()
    {
        EventDetailViewModel sut = CreateLoadedSut(CreateDetailWithLinks(2));

        Assert.Equal("LINKS (2)", sut.LinksHeadingText);
        Assert.True(sut.HasLinks);
    }

    [Fact]
    public void LinksHeadingText_ShowsOne_ForAnEventWithOneLink()
    {
        EventDetailViewModel sut = CreateLoadedSut(CreateDetailWithLinks(1));

        Assert.Equal("LINKS (1)", sut.LinksHeadingText);
        Assert.True(sut.HasLinks);
    }

    [Fact]
    public void LinksHeadingText_IsEmptyAndHidden_WhenTheLinksListIsEmpty()
    {
        EventDetailViewModel sut = CreateLoadedSut(CreateDetailWithLinks(0));

        Assert.Equal(string.Empty, sut.LinksHeadingText);
        Assert.False(sut.HasLinks);
    }

    [Fact]
    public void LinksHeadingText_IsEmpty_WhenNoEventIsLoaded()
    {
        EventDetailViewModel sut = CreateSut();

        Assert.Equal(string.Empty, sut.LinksHeadingText);
        Assert.False(sut.HasLinks);
    }

    [Fact]
    public void Detail_RaisesLinksHeadingTextNotification()
    {
        EventDetailViewModel sut = CreateSut();
        List<string?> raised = [];
        sut.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        sut.Detail = CreateDetailWithLinks(2);

        Assert.Contains(nameof(EventDetailViewModel.LinksHeadingText), raised);
        Assert.Equal("LINKS (2)", sut.LinksHeadingText);
    }

    [Fact]
    public void LinksHeadingText_FollowsDetail_WhenReplacedWithAnEventWithoutLinks()
    {
        EventDetailViewModel sut = CreateLoadedSut(CreateDetailWithLinks(2));
        Assert.Equal("LINKS (2)", sut.LinksHeadingText);
        List<string?> raised = [];
        sut.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        sut.Detail = CreateDetailWithLinks(0);

        Assert.Equal(string.Empty, sut.LinksHeadingText);
        Assert.False(sut.HasLinks);
        Assert.Contains(nameof(EventDetailViewModel.LinksHeadingText), raised);
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

    // ── Copy as JSON ──────────────────────────────────────────────────────────

    [Fact]
    public void NewtonsoftJson_IsThePinnedVersion()
    {
        // The test host may resolve a newer Newtonsoft.Json at runtime, so check the app project's reference.
        string? dir = AppContext.BaseDirectory;
        string? csproj = null;
        while (dir is not null)
        {
            string candidate = Path.Combine(dir, "VenueOps", "VenueOps.csproj");
            if (File.Exists(candidate))
            {
                csproj = candidate;
                break;
            }
            dir = Path.GetDirectoryName(dir);
        }

        Assert.NotNull(csproj);
        var reference = System.Xml.Linq.XDocument.Load(csproj)
            .Descendants("PackageReference")
            .Where(e => (string?)e.Attribute("Include") == "Newtonsoft.Json")
            .ToList();

        Assert.Single(reference);
        Assert.Equal("12.0.3", (string?)reference[0].Attribute("Version"));
    }

    [Fact]
    public async Task CopyAsJsonCommand_CopiesBuiltJsonToClipboard()
    {
        EventDetail detail = CreateDetail();
        _clipboardMock.Setup(c => c.SetTextAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
        EventDetailViewModel sut = CreateLoadedSut(detail);

        await sut.CopyAsJsonCommand.ExecuteAsync(null);

        _clipboardMock.Verify(c => c.SetTextAsync(EventDetailViewModel.BuildEventJson(detail)), Times.Once);
    }

    [Fact]
    public void BuildEventJson_IsIndented()
    {
        string json = EventDetailViewModel.BuildEventJson(CreateDetail());

        Assert.Contains("\n", json);
        Assert.Contains("  \"eventName\"", json);
    }

    [Fact]
    public void BuildEventJson_UsesCamelCaseNames_AndNoPascalCaseNames()
    {
        var obj = Newtonsoft.Json.Linq.JObject.Parse(EventDetailViewModel.BuildEventJson(CreateDetail()));
        List<string> names = obj.Properties().Select(p => p.Name).ToList();

        foreach (string expected in new[]
        {
            "id", "eventName", "venueName", "status", "startDateUtc", "endDateUtc",
            "expectedAtendees", "attendeeCount", "bookingReference", "shortDescription",
            "images", "location", "organizer", "tags", "links"
        })
        {
            Assert.Contains(expected, names);
        }

        Assert.DoesNotContain(names, n => char.IsUpper(n[0]));
        Assert.Equal("Aurora Night Market", (string?)obj["eventName"]);
    }

    [Fact]
    public void BuildEventJson_WritesUtcDateAsIso8601()
    {
        EventDetail detail = CreateDetail();
        detail.StartDateUtc = new DateTime(2025, 3, 4, 5, 6, 7, DateTimeKind.Utc);

        string json = EventDetailViewModel.BuildEventJson(detail);

        Assert.Contains("\"startDateUtc\": \"2025-03-04T05:06:07Z\"", json);
    }

    [Fact]
    public void BuildEventJson_ConvertsLocalDateToUtc()
    {
        EventDetail detail = CreateDetail();
        DateTime local = new(2025, 3, 4, 5, 6, 7, DateTimeKind.Local);
        detail.EndDateUtc = local;

        string json = EventDetailViewModel.BuildEventJson(detail);

        string expected = local.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
        Assert.Contains($"\"endDateUtc\": \"{expected}\"", json);
    }

    [Fact]
    public void BuildEventJson_IncludesNestedValues()
    {
        var obj = Newtonsoft.Json.Linq.JObject.Parse(EventDetailViewModel.BuildEventJson(CreateDetail()));

        Assert.Equal("3 Example Quay", (string?)obj["location"]!["formattedAddress"]);
        Assert.Equal("Night Markets Ltd", (string?)obj["organizer"]!["name"]);
        List<string?> tags = obj["tags"]!.Select(t => (string?)t).ToList();
        Assert.Equal(2, tags.Count);
        Assert.Equal("food", tags[0]);
        Assert.Equal("music", tags[1]);
        Assert.Equal("Map", (string?)obj["links"]![0]!["label"]);
        Assert.Equal(3, obj["images"]!.Count());
    }

    [Fact]
    public void BuildEventJson_WritesNullOptionalObjectsAsNull()
    {
        EventDetail detail = CreateDetail();
        detail.Location = null;
        detail.Organizer = null;

        var obj = Newtonsoft.Json.Linq.JObject.Parse(EventDetailViewModel.BuildEventJson(detail));

        Assert.NotNull(obj.Property("location"));
        Assert.NotNull(obj.Property("organizer"));
        Assert.Equal(Newtonsoft.Json.Linq.JTokenType.Null, obj["location"]!.Type);
        Assert.Equal(Newtonsoft.Json.Linq.JTokenType.Null, obj["organizer"]!.Type);
    }

    [Fact]
    public void CopyAsJsonCommand_CanExecuteFollowsDetail()
    {
        EventDetailViewModel sut = CreateSut();
        int raised = 0;
        sut.CopyAsJsonCommand.CanExecuteChanged += (_, _) => raised++;

        Assert.False(sut.CopyAsJsonCommand.CanExecute(null));

        sut.Detail = CreateDetail();

        Assert.True(sut.CopyAsJsonCommand.CanExecute(null));
        Assert.True(raised > 0);
    }

    // ── Copy as JSON: packages, shape, names, dates, culture, command behaviour ──

    private const string IsoUtcPattern = @"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d{1,7})?Z$";

    private static string FindAppProjectPath()
    {
        string? dir = AppContext.BaseDirectory;
        while (dir is not null)
        {
            string candidate = Path.Combine(dir, "VenueOps", "VenueOps.csproj");
            if (File.Exists(candidate)) return candidate;
            dir = Path.GetDirectoryName(dir);
        }
        throw new FileNotFoundException("VenueOps/VenueOps.csproj was not found above the test output folder.");
    }

    private static string CamelCase(string pascal) => char.ToLowerInvariant(pascal[0]) + pascal[1..];

    private static List<string> CamelCaseNamesOf(Type type) =>
        type.GetProperties().Select(p => CamelCase(p.Name)).OrderBy(n => n, StringComparer.Ordinal).ToList();

    private static List<string> PropertyNamesOf(JObject obj) =>
        obj.Properties().Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal).ToList();

    // Reads the raw text of a date property. JObject.Parse would turn it into a DateTime, hiding its format.
    private static string RawDateValue(string json, string propertyName)
    {
        var match = Regex.Match(json, $"\"{propertyName}\": \"([^\"]*)\"");
        Assert.True(match.Success, $"'{propertyName}' is not written as a string.");
        return match.Groups[1].Value;
    }

    private static void WithCurrentCulture(CultureInfo culture, Action assert)
    {
        CultureInfo original = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = culture;
        try { assert(); }
        finally { CultureInfo.CurrentCulture = original; }
    }

    [Fact]
    public void AppProject_PackageAndProjectReferences_AreExactlyTheKnownSet()
    {
        XDocument project = XDocument.Load(FindAppProjectPath());

        List<string> actual = project.Descendants("PackageReference")
            .Select(e => $"{(string?)e.Attribute("Include")}@{(string?)e.Attribute("Version")}")
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToList();
        List<string> expected = new List<string>
        {
            "CommunityToolkit.Maui@14.1.0",
            "CommunityToolkit.Mvvm@8.4.2",
            "Microsoft.Extensions.Http@10.0.6",
            "Microsoft.Maui.Controls@$(MauiVersion)",
            "Microsoft.Extensions.Logging.Debug@10.0.0",
            "Newtonsoft.Json@12.0.3",
            "SonarAnalyzer.CSharp@10.35.0.4138"
        }.OrderBy(s => s, StringComparer.Ordinal).ToList();

        Assert.Equal(expected, actual);
        Assert.Empty(project.Descendants("ProjectReference"));
        Assert.Equal("10.0.51", project.Descendants("MauiVersion").Single().Value);
    }

    [Fact]
    public async Task CopyAsJsonCommand_CopiesIndentedCamelCaseJsonOfTheLoadedEvent()
    {
        EventDetail detail = CreateDetail();
        string? copied = null;
        _clipboardMock
            .Setup(c => c.SetTextAsync(It.IsAny<string>()))
            .Callback<string>(text => copied = text)
            .Returns(Task.CompletedTask);
        EventDetailViewModel sut = CreateLoadedSut(detail);

        await sut.CopyAsJsonCommand.ExecuteAsync(null);

        Assert.NotNull(copied);
        Assert.Equal(EventDetailViewModel.BuildEventJson(detail), copied);
        Assert.Contains("\n  \"eventName\": \"Aurora Night Market\"", copied);
        Assert.Contains("\"expectedAtendees\": 1200", copied);
    }

    [Fact]
    public void BuildEventJson_IndentsNestedPropertiesByTwoSpacesPerLevel()
    {
        string json = EventDetailViewModel.BuildEventJson(CreateDetail());

        Assert.Contains("\n    \"formattedAddress\": \"3 Example Quay\"", json);
    }

    [Fact]
    public void BuildEventJson_WritesEveryEventDetailPropertyWithCamelCaseNames()
    {
        JObject obj = JObject.Parse(EventDetailViewModel.BuildEventJson(CreateDetail()));

        Assert.Equal(CamelCaseNamesOf(typeof(EventDetail)), PropertyNamesOf(obj));
        Assert.Contains("expectedAtendees", PropertyNamesOf(obj));
        Assert.DoesNotContain("expectedAttendees", PropertyNamesOf(obj));
        Assert.Contains("attendeeCount", PropertyNamesOf(obj));
    }

    [Fact]
    public void BuildEventJson_WritesNestedObjectsWithCamelCaseNames()
    {
        JObject obj = JObject.Parse(EventDetailViewModel.BuildEventJson(CreateDetail()));

        Assert.Equal(CamelCaseNamesOf(typeof(EventImage)), PropertyNamesOf((JObject)obj["images"]![0]!));
        Assert.Equal(CamelCaseNamesOf(typeof(EventLocation)), PropertyNamesOf((JObject)obj["location"]!));
        Assert.Equal(CamelCaseNamesOf(typeof(EventOrganizer)), PropertyNamesOf((JObject)obj["organizer"]!));
        Assert.Equal(CamelCaseNamesOf(typeof(EventLink)), PropertyNamesOf((JObject)obj["links"]![0]!));
        Assert.DoesNotContain(obj.Descendants().OfType<JProperty>().Select(p => p.Name), n => char.IsUpper(n[0]));
    }

    [Fact]
    public void BuildEventJson_WritesEndDateAsIso8601Utc()
    {
        EventDetail detail = CreateDetail();
        detail.EndDateUtc = new DateTime(2025, 3, 4, 5, 6, 7, DateTimeKind.Utc);

        string json = EventDetailViewModel.BuildEventJson(detail);

        Assert.Contains("\"endDateUtc\": \"2025-03-04T05:06:07Z\"", json);
    }

    [Fact]
    public void BuildEventJson_WritesLocalDateAsTheSameInstantInUtc()
    {
        // The app runs in the host's time zone; the instant is only shifted when the host is not on UTC.
        EventDetail detail = CreateDetail();
        DateTime local = new(2025, 3, 4, 5, 6, 7, DateTimeKind.Local);
        detail.EndDateUtc = local;

        string written = RawDateValue(EventDetailViewModel.BuildEventJson(detail), "endDateUtc");

        Assert.EndsWith("Z", written);
        Assert.Equal(local.ToUniversalTime(), DateTimeOffset.Parse(written, CultureInfo.InvariantCulture).UtcDateTime);
    }

    [Fact]
    public void BuildEventJson_WritesUnspecifiedDateAsIso8601EndingInZ()
    {
        // The criterion does not say whether the clock value is kept or shifted, only that the text ends in Z.
        EventDetail detail = CreateDetail();
        detail.StartDateUtc = new DateTime(2025, 3, 4, 5, 6, 7, DateTimeKind.Unspecified);

        string written = RawDateValue(EventDetailViewModel.BuildEventJson(detail), "startDateUtc");

        Assert.Matches(IsoUtcPattern, written);
    }

    [Fact]
    public void BuildEventJson_WritesFractionalSecondsAsIso8601Utc()
    {
        // The criterion does not say whether the fraction is kept or truncated; the instant must be unchanged.
        EventDetail detail = CreateDetail();
        DateTime start = new(2025, 3, 4, 5, 6, 7, 500, DateTimeKind.Utc);
        detail.StartDateUtc = start;

        string written = RawDateValue(EventDetailViewModel.BuildEventJson(detail), "startDateUtc");

        Assert.Matches(IsoUtcPattern, written);
        Assert.Equal(start, DateTimeOffset.Parse(written, CultureInfo.InvariantCulture).UtcDateTime);
    }

    [Fact]
    public void BuildEventJson_DatesMatchTheIso8601UtcPattern()
    {
        string json = EventDetailViewModel.BuildEventJson(CreateDetail());

        Assert.Matches(IsoUtcPattern, RawDateValue(json, "startDateUtc"));
        Assert.Matches(IsoUtcPattern, RawDateValue(json, "endDateUtc"));
    }

    [Fact]
    public void BuildEventJson_KeepsEveryPropertyAndWritesEmptySectionsAsNullOrEmptyArrays()
    {
        EventDetail detail = CreateDetail();
        detail.Location = null;
        detail.Organizer = null;
        detail.Images = [];
        detail.Tags = [];
        detail.Links = [];

        JObject obj = JObject.Parse(EventDetailViewModel.BuildEventJson(detail));

        Assert.Equal(CamelCaseNamesOf(typeof(EventDetail)), PropertyNamesOf(obj));
        Assert.Equal(JTokenType.Null, obj["location"]!.Type);
        Assert.Equal(JTokenType.Null, obj["organizer"]!.Type);
        Assert.Equal(JTokenType.Array, obj["images"]!.Type);
        Assert.Empty((JArray)obj["images"]!);
        Assert.Empty((JArray)obj["tags"]!);
        Assert.Empty((JArray)obj["links"]!);
    }

    [Theory]
    [InlineData("de-DE")]
    [InlineData("ar-SA")]
    public void BuildEventJson_DoesNotDependOnTheCurrentCulture(string cultureName)
    {
        EventDetail detail = CreateDetail();
        string expected = string.Empty;
        string actual = string.Empty;
        WithCurrentCulture(CultureInfo.InvariantCulture, () => expected = EventDetailViewModel.BuildEventJson(detail));
        WithCurrentCulture(new CultureInfo(cultureName), () => actual = EventDetailViewModel.BuildEventJson(detail));

        Assert.Equal(expected, actual);
        Assert.Contains("\"expectedAtendees\": 1200", actual);
        Assert.Contains("\"startDateUtc\": \"2030-06-01T11:00:00Z\"", actual);
    }

    [Fact]
    public async Task CopyAsJsonCommand_ClipboardFailure_IsSurfacedToTheCaller()
    {
        // The criterion does not state the failure behaviour. This records the actual one: the exception surfaces.
        var failure = new InvalidOperationException("clipboard unavailable");
        _clipboardMock.Setup(c => c.SetTextAsync(It.IsAny<string>())).ThrowsAsync(failure);
        EventDetailViewModel sut = CreateLoadedSut(CreateDetail());

        Exception? thrown = await Record.ExceptionAsync(() => sut.CopyAsJsonCommand.ExecuteAsync(null));

        Assert.Same(failure, thrown);
        Assert.True(sut.CopyAsJsonCommand.CanExecute(null));
    }

    [Fact]
    public async Task CopyAsJsonCommand_RunTwiceInARow_CopiesTheFullJsonEachTime()
    {
        EventDetail detail = CreateDetail();
        string expected = EventDetailViewModel.BuildEventJson(detail);
        var copies = new List<string>();
        _clipboardMock
            .Setup(c => c.SetTextAsync(It.IsAny<string>()))
            .Callback<string>(copies.Add)
            .Returns(Task.CompletedTask);
        EventDetailViewModel sut = CreateLoadedSut(detail);

        await sut.CopyAsJsonCommand.ExecuteAsync(null);
        await sut.CopyAsJsonCommand.ExecuteAsync(null);

        Assert.Equal(new[] { expected, expected }, copies);
    }

    [Fact]
    public async Task CopyAsJsonCommand_RunTwiceConcurrently_EveryCopiedTextIsTheFullJson()
    {
        // The criterion does not state how overlapping runs are guarded, so the number of copies is not asserted.
        EventDetail detail = CreateDetail();
        string expected = EventDetailViewModel.BuildEventJson(detail);
        var gate = new TaskCompletionSource();
        var copies = new List<string>();
        _clipboardMock
            .Setup(c => c.SetTextAsync(It.IsAny<string>()))
            .Callback<string>(copies.Add)
            .Returns(() => gate.Task);
        EventDetailViewModel sut = CreateLoadedSut(detail);

        Task first = sut.CopyAsJsonCommand.ExecuteAsync(null);
        Task second = sut.CopyAsJsonCommand.ExecuteAsync(null);
        gate.SetResult();
        await Task.WhenAll(first, second);

        Assert.NotEmpty(copies);
        Assert.All(copies, text => Assert.Equal(expected, text));
    }

    [Fact]
    public void CopyAsJsonCommand_IsDisabledWithNoEvent_AndRaisesChangeWhenAnEventIsLoaded()
    {
        EventDetailViewModel sut = CreateSut();
        int raised = 0;
        sut.CopyAsJsonCommand.CanExecuteChanged += (_, _) => raised++;

        Assert.False(sut.CopyAsJsonCommand.CanExecute(null));

        int before = raised;
        sut.Detail = CreateDetail();

        Assert.True(raised > before);
        Assert.True(sut.CopyAsJsonCommand.CanExecute(null));
    }

    [Fact]
    public async Task CopyAsJsonCommand_IsDisabled_WhenInitializeAsyncReturnsNull()
    {
        SetupDetail(CreateDetail());
        EventDetailViewModel sut = CreateSut();
        sut.ApplyQueryAttributes(new Dictionary<string, object> { ["id"] = "evt-500" });
        await sut.InitializeAsync();
        Assert.True(sut.CopyAsJsonCommand.CanExecute(null));

        SetupDetail(null);
        await sut.InitializeAsync();

        Assert.False(sut.CopyAsJsonCommand.CanExecute(null));
    }

    [Fact]
    public async Task CopyAsJsonCommand_IsDisabled_WhenInitializeAsyncThrows()
    {
        SetupDetail(CreateDetail());
        EventDetailViewModel sut = CreateSut();
        sut.ApplyQueryAttributes(new Dictionary<string, object> { ["id"] = "evt-500" });
        await sut.InitializeAsync();
        Assert.True(sut.CopyAsJsonCommand.CanExecute(null));

        _eventsMock
            .Setup(e => e.GetEventDetailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("offline"));
        await sut.InitializeAsync();

        Assert.False(sut.CopyAsJsonCommand.CanExecute(null));
    }

    [Fact]
    public void CopyAsJsonCommand_IsDisabled_WhenDetailIsClearedDirectly()
    {
        EventDetailViewModel sut = CreateLoadedSut(CreateDetail());
        Assert.True(sut.CopyAsJsonCommand.CanExecute(null));

        sut.Detail = null;

        Assert.False(sut.CopyAsJsonCommand.CanExecute(null));
    }

    [Fact]
    public async Task CopyAsJsonCommand_ForcedExecutionWithNoEvent_CopiesNothing()
    {
        // The criterion does not state the outcome of a forced run; it must not copy a null or "null" text.
        EventDetailViewModel sut = CreateSut();

        await sut.CopyAsJsonCommand.ExecuteAsync(null);

        _clipboardMock.Verify(c => c.SetTextAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void CopyAsJsonCommand_RaisesCanExecuteChanged_WhenTheLoadedEventIsCleared()
    {
        EventDetailViewModel sut = CreateLoadedSut(CreateDetail());
        Assert.True(sut.CopyAsJsonCommand.CanExecute(null));
        int raised = 0;
        sut.CopyAsJsonCommand.CanExecuteChanged += (_, _) => raised++;

        sut.Detail = null;

        Assert.False(sut.CopyAsJsonCommand.CanExecute(null));
        Assert.True(raised > 0);
    }

    [Fact]
    public async Task CopyAsJsonCommand_AfterTheEventIsReplaced_CopiesTheCurrentEventOnly()
    {
        EventDetail first = CreateDetail();
        EventDetail second = CreateDetail();
        second.Id = "evt-501";
        second.EventName = "Harbour Lights Fair";
        var copies = new List<string>();
        _clipboardMock
            .Setup(c => c.SetTextAsync(It.IsAny<string>()))
            .Callback<string>(copies.Add)
            .Returns(Task.CompletedTask);
        EventDetailViewModel sut = CreateLoadedSut(first);

        sut.Detail = second;
        await sut.CopyAsJsonCommand.ExecuteAsync(null);

        _clipboardMock.Verify(c => c.SetTextAsync(It.IsAny<string>()), Times.Once);
        Assert.Single(copies);
        Assert.Equal(EventDetailViewModel.BuildEventJson(second), copies[0]);
        Assert.Contains("\"eventName\": \"Harbour Lights Fair\"", copies[0]);
        Assert.DoesNotContain("Aurora Night Market", copies[0]);
    }

    [Fact]
    public void BuildEventJson_WritesEmptyListsAsArraysAndEmptyBookingReferenceAsEmptyString()
    {
        EventDetail detail = CreateDetail();
        detail.Images = [];
        detail.Tags = [];
        detail.Links = [];
        detail.BookingReference = string.Empty;

        JObject obj = JObject.Parse(EventDetailViewModel.BuildEventJson(detail));

        Assert.Equal(CamelCaseNamesOf(typeof(EventDetail)), PropertyNamesOf(obj));
        Assert.Equal(JTokenType.Array, obj["images"]!.Type);
        Assert.Equal(JTokenType.Array, obj["tags"]!.Type);
        Assert.Equal(JTokenType.Array, obj["links"]!.Type);
        Assert.Empty((JArray)obj["images"]!);
        Assert.Empty((JArray)obj["tags"]!);
        Assert.Empty((JArray)obj["links"]!);
        Assert.Equal(JTokenType.String, obj["bookingReference"]!.Type);
        Assert.Equal(string.Empty, (string?)obj["bookingReference"]);
    }

    [Fact]
    public async Task CopyAsJsonCommand_IsDisabledDuringReload_AndEnabledAfterTheEventLoads()
    {
        SetupDetail(CreateDetail());
        EventDetailViewModel sut = CreateSut();
        sut.ApplyQueryAttributes(new Dictionary<string, object> { ["id"] = "evt-500" });
        await sut.InitializeAsync();
        Assert.True(sut.CopyAsJsonCommand.CanExecute(null));

        var pending = new TaskCompletionSource<EventDetail?>();
        _eventsMock
            .Setup(e => e.GetEventDetailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(pending.Task);

        Task reload = sut.InitializeAsync();
        Assert.False(sut.CopyAsJsonCommand.CanExecute(null));

        pending.SetResult(CreateDetail());
        await reload;

        Assert.True(sut.CopyAsJsonCommand.CanExecute(null));
    }
}
