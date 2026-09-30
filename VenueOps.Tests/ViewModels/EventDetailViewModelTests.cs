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
