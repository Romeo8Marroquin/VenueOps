using CommunityToolkit.Maui;
using Moq;
using VenueOps.Models.Events;
using VenueOps.Services;
using VenueOps.ViewModels;

namespace VenueOps.Tests.ViewModels;

public class EditEventViewModelTests
{
    private readonly Mock<IEventsService> _eventsMock = new();
    private readonly Mock<IPopupService> _popupMock = new();
    private UpdateEventRequest? _sent;

    public EditEventViewModelTests()
    {
        _eventsMock
            .Setup(e => e.UpdateEventAsync(It.IsAny<UpdateEventRequest>(), It.IsAny<CancellationToken>()))
            .Callback<UpdateEventRequest, CancellationToken>((r, _) => _sent = r)
            .ReturnsAsync(new EventDetail { Id = "evt-800", EventName = "Cedar Grove Concert", BookingReference = "BK-0800" });
    }

    private EditEventViewModel CreateSut() => new(_eventsMock.Object, _popupMock.Object);

    private static EventDetail CreateDetail() => new()
    {
        Id = "evt-800",
        EventName = "Cedar Grove Concert",
        VenueName = "Grove Amphitheatre",
        Status = "confirmed",
        StartDateUtc = new DateTime(2030, 7, 4, 18, 0, 0, DateTimeKind.Utc),
        EndDateUtc = new DateTime(2030, 7, 4, 21, 30, 0, DateTimeKind.Utc),
        ExpectedAtendees = 900,
        ShortDescription = "A fictional summer concert.",
        Location = new EventLocation { FormattedAddress = "5 Example Grove" },
        Organizer = new EventOrganizer { Name = "Grove Society", WebsiteUrl = "https://grove.example.invalid" },
        Tags = ["music", "summer"],
        Links = [new EventLink { Label = "Tickets", Url = "https://tickets.example.invalid/800" }],
        Images = [new EventImage { Url = "https://images.example.invalid/800.jpg", AltText = "Stage", Credit = "Studio" }]
    };

    private EditEventViewModel CreateLoadedSut(EventDetail? detail = null)
    {
        EditEventViewModel sut = CreateSut();
        sut.LoadFromDetail(detail ?? CreateDetail());
        return sut;
    }

    private void VerifyPopupClosed(bool result, Times times) =>
        _popupMock.Verify(p => p.ClosePopupAsync(It.IsAny<Page>(), result, It.IsAny<CancellationToken>()), times);

    // ── Initial state ─────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_SetsDefaults()
    {
        EditEventViewModel sut = CreateSut();

        Assert.Equal("Edit Event", sut.Title);
        Assert.Equal("Draft", sut.Status);
        Assert.Equal(new TimeSpan(9, 0, 0), sut.StartTime);
        Assert.Equal(new TimeSpan(10, 0, 0), sut.EndTime);
        Assert.False(sut.HasError);
        Assert.True(sut.IsCompactLayout);
        Assert.Equal(["Draft", "Confirmed", "Cancelled", "Completed"], sut.StatusOptions);
    }

    [Fact]
    public void IsWideLayout_TogglesCompactLayoutAndNotifies()
    {
        EditEventViewModel sut = CreateSut();
        List<string?> raised = [];
        sut.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        sut.IsWideLayout = true;

        Assert.False(sut.IsCompactLayout);
        Assert.Contains(nameof(EditEventViewModel.IsCompactLayout), raised);
    }

    // ── LoadFromDetail ────────────────────────────────────────────────────────

    [Fact]
    public void LoadFromDetail_PopulatesEveryField()
    {
        EventDetail detail = CreateDetail();
        DateTime start = detail.StartDateUtc.ToLocalTime();
        DateTime end = detail.EndDateUtc.ToLocalTime();

        EditEventViewModel sut = CreateLoadedSut(detail);

        Assert.Equal("Cedar Grove Concert", sut.EventName);
        Assert.Equal("Grove Amphitheatre", sut.VenueName);
        Assert.Equal("Confirmed", sut.Status);
        Assert.Equal("900", sut.ExpectedAttendeesText);
        Assert.Equal(start.Date, sut.StartDate);
        Assert.Equal(start.TimeOfDay, sut.StartTime);
        Assert.Equal(end.Date, sut.EndDate);
        Assert.Equal(end.TimeOfDay, sut.EndTime);
        Assert.Equal("A fictional summer concert.", sut.ShortDescription);
        Assert.Equal("5 Example Grove", sut.LocationAddress);
        Assert.Equal("Grove Society", sut.OrganizerName);
        Assert.Equal("https://grove.example.invalid", sut.OrganizerWebsite);
        Assert.Equal("music, summer", sut.TagsText);
        LinkItem link = Assert.Single(sut.Links);
        Assert.Equal("Tickets", link.Label);
        Assert.Equal("https://tickets.example.invalid/800", link.Url);
        ImageItem image = Assert.Single(sut.Images);
        Assert.Equal("https://images.example.invalid/800.jpg", image.Url);
        Assert.Equal("Stage", image.AltText);
        Assert.Equal("Studio", image.Credit);
    }

    [Theory]
    [InlineData("confirmed", "Confirmed")]
    [InlineData("cancelled", "Cancelled")]
    [InlineData("completed", "Completed")]
    [InlineData("draft", "Draft")]
    [InlineData("unknown", "Draft")]
    public void LoadFromDetail_MapsApiStatusToOption(string apiStatus, string expected)
    {
        EventDetail detail = CreateDetail();
        detail.Status = apiStatus;

        Assert.Equal(expected, CreateLoadedSut(detail).Status);
    }

    [Fact]
    public void LoadFromDetail_UsesEmptyValues_WhenOptionalSectionsAreMissing()
    {
        // Mirrors a payload where the API sends null for optional sections.
        EventDetail detail = CreateDetail();
        detail.ExpectedAtendees = 0;
        detail.ShortDescription = null!;
        detail.Location = null;
        detail.Organizer = null;
        detail.Tags = null!;
        detail.Links = [];
        detail.Images = [];

        EditEventViewModel sut = CreateLoadedSut(detail);

        Assert.Equal(string.Empty, sut.ExpectedAttendeesText);
        Assert.Equal(string.Empty, sut.ShortDescription);
        Assert.Equal(string.Empty, sut.LocationAddress);
        Assert.Equal(string.Empty, sut.OrganizerName);
        Assert.Equal(string.Empty, sut.OrganizerWebsite);
        Assert.Equal(string.Empty, sut.TagsText);
        Assert.Empty(sut.Links);
        Assert.Empty(sut.Images);
    }

    [Fact]
    public void LoadFromDetail_UsesEmptyValues_WhenNestedValuesAreMissing()
    {
        EventDetail detail = CreateDetail();
        detail.Location = new EventLocation { FormattedAddress = null! };
        detail.Organizer = new EventOrganizer { Name = null!, WebsiteUrl = null };
        detail.Tags = [];

        EditEventViewModel sut = CreateLoadedSut(detail);

        Assert.Equal(string.Empty, sut.LocationAddress);
        Assert.Equal(string.Empty, sut.OrganizerName);
        Assert.Equal(string.Empty, sut.OrganizerWebsite);
        Assert.Equal(string.Empty, sut.TagsText);
    }

    [Fact]
    public void LoadFromDetail_ReplacesPreviouslyLoadedItems()
    {
        EditEventViewModel sut = CreateLoadedSut();
        sut.AddLinkCommand.Execute(null);
        sut.AddImageCommand.Execute(null);

        sut.LoadFromDetail(CreateDetail());

        Assert.Single(sut.Links);
        Assert.Single(sut.Images);
    }

    // ── Collections ───────────────────────────────────────────────────────────

    [Fact]
    public void CollectionCommands_AddAndRemoveItems()
    {
        EditEventViewModel sut = CreateSut();

        sut.AddLinkCommand.Execute(null);
        sut.AddImageCommand.Execute(null);
        sut.RemoveLinkCommand.Execute(sut.Links[0]);
        sut.RemoveImageCommand.Execute(sut.Images[0]);

        Assert.Empty(sut.Links);
        Assert.Empty(sut.Images);
    }

    // ── Validation ────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("", "Grove Amphitheatre", "Event name is required.")]
    [InlineData("  ", "Grove Amphitheatre", "Event name is required.")]
    [InlineData("Cedar Grove Concert", "", "Venue name is required.")]
    [InlineData("Cedar Grove Concert", " ", "Venue name is required.")]
    public async Task SubmitCommand_RequiresNames(string eventName, string venueName, string expectedError)
    {
        EditEventViewModel sut = CreateLoadedSut();
        sut.EventName = eventName;
        sut.VenueName = venueName;

        await sut.SubmitCommand.ExecuteAsync(null);

        Assert.Equal(expectedError, sut.ErrorMessage);
        _eventsMock.Verify(e => e.UpdateEventAsync(It.IsAny<UpdateEventRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SubmitCommand_RequiresEndAfterStart()
    {
        EditEventViewModel sut = CreateLoadedSut();
        sut.EndDate = sut.StartDate;
        sut.EndTime = sut.StartTime;

        await sut.SubmitCommand.ExecuteAsync(null);

        Assert.Equal("End date/time must be after start date/time.", sut.ErrorMessage);
    }

    [Theory]
    [InlineData("many")]
    [InlineData("-5")]
    public async Task SubmitCommand_RejectsInvalidAttendeeCount(string attendees)
    {
        EditEventViewModel sut = CreateLoadedSut();
        sut.ExpectedAttendeesText = attendees;

        await sut.SubmitCommand.ExecuteAsync(null);

        Assert.Equal("Expected attendees must be a non-negative number.", sut.ErrorMessage);
        _eventsMock.Verify(e => e.UpdateEventAsync(It.IsAny<UpdateEventRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Event name length ─────────────────────────────────────────────────────

    private const string NameTooLongMessage = "Event name must be 80 characters or fewer.";

    // The service returns null so the post-save code that needs Application.Current and Shell.Current is not reached.
    private void SetupUpdateReturnsNull() =>
        _eventsMock
            .Setup(e => e.UpdateEventAsync(It.IsAny<UpdateEventRequest>(), It.IsAny<CancellationToken>()))
            .Callback<UpdateEventRequest, CancellationToken>((r, _) => _sent = r)
            .ReturnsAsync((EventDetail?)null);

    private void VerifyUpdateNeverCalled() =>
        _eventsMock.Verify(e => e.UpdateEventAsync(It.IsAny<UpdateEventRequest>(), It.IsAny<CancellationToken>()), Times.Never);

    private void VerifyUpdateCalledOnceWithName(string expectedName)
    {
        _eventsMock.Verify(e => e.UpdateEventAsync(It.IsAny<UpdateEventRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.NotNull(_sent);
        Assert.Equal(expectedName, _sent.EventName);
    }

    [Fact]
    public async Task SubmitCommand_RejectsEventNameOver80Characters()
    {
        EditEventViewModel sut = CreateLoadedSut();
        sut.EventName = new string('a', 81);

        await sut.SubmitCommand.ExecuteAsync(null);

        Assert.Equal(NameTooLongMessage, sut.ErrorMessage);
        Assert.True(sut.HasError);
        VerifyUpdateNeverCalled();
    }

    [Fact]
    public async Task SubmitCommand_RejectsEventNameOver80Characters_WhenTrimmedAndPaddedWithSpaces()
    {
        EditEventViewModel sut = CreateLoadedSut();
        sut.EventName = "   " + new string('b', 81) + "  ";

        await sut.SubmitCommand.ExecuteAsync(null);

        Assert.Equal(NameTooLongMessage, sut.ErrorMessage);
        Assert.True(sut.HasError);
        VerifyUpdateNeverCalled();
    }

    [Fact]
    public async Task SubmitCommand_SavesEventNameOfExactly80Characters()
    {
        SetupUpdateReturnsNull();
        string name = new('c', 80);
        EditEventViewModel sut = CreateLoadedSut();
        sut.EventName = name;

        await sut.SubmitCommand.ExecuteAsync(null);

        VerifyUpdateCalledOnceWithName(name);
        Assert.NotEqual(NameTooLongMessage, sut.ErrorMessage);
    }

    [Fact]
    public async Task SubmitCommand_IgnoresSpacesAroundEventNameWhenCountingLength()
    {
        SetupUpdateReturnsNull();
        string name = new('d', 80);
        EditEventViewModel sut = CreateLoadedSut();
        sut.EventName = "    " + name + "   ";

        await sut.SubmitCommand.ExecuteAsync(null);

        VerifyUpdateCalledOnceWithName(name);
        Assert.NotEqual(NameTooLongMessage, sut.ErrorMessage);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SubmitCommand_ShowsRequiredMessageRatherThanLengthMessage_WhenNameIsBlank(string eventName)
    {
        EditEventViewModel sut = CreateLoadedSut();
        sut.EventName = eventName;

        await sut.SubmitCommand.ExecuteAsync(null);

        Assert.Equal("Event name is required.", sut.ErrorMessage);
        Assert.NotEqual(NameTooLongMessage, sut.ErrorMessage);
        VerifyUpdateNeverCalled();
    }

    [Fact]
    public async Task SubmitCommand_ChecksEventNameLengthBeforeVenueName()
    {
        EditEventViewModel sut = CreateLoadedSut();
        sut.EventName = new string('e', 81);
        sut.VenueName = string.Empty;

        await sut.SubmitCommand.ExecuteAsync(null);

        Assert.Equal(NameTooLongMessage, sut.ErrorMessage);
        VerifyUpdateNeverCalled();
    }

    [Fact]
    public async Task SubmitCommand_KeepsVenueNameCheck_WhenEventNameIs80Characters()
    {
        EditEventViewModel sut = CreateLoadedSut();
        sut.EventName = new string('f', 80);
        sut.VenueName = string.Empty;

        await sut.SubmitCommand.ExecuteAsync(null);

        Assert.Equal("Venue name is required.", sut.ErrorMessage);
        VerifyUpdateNeverCalled();
    }

    [Fact]
    public async Task SubmitCommand_KeepsEndAfterStartCheck_WhenEventNameIs80Characters()
    {
        EditEventViewModel sut = CreateLoadedSut();
        sut.EventName = new string('g', 80);
        sut.EndDate = sut.StartDate.AddDays(-1);

        await sut.SubmitCommand.ExecuteAsync(null);

        Assert.Equal("End date/time must be after start date/time.", sut.ErrorMessage);
        VerifyUpdateNeverCalled();
    }

    [Theory]
    [InlineData(0, "Event name is required.", false)]
    [InlineData(1, null, true)]
    [InlineData(79, null, true)]
    [InlineData(80, null, true)]
    [InlineData(81, NameTooLongMessage, false)]
    [InlineData(200, NameTooLongMessage, false)]
    public async Task SubmitCommand_AppliesEventNameLengthLimit(int trimmedLength, string? expectedError, bool expectedSaved)
    {
        SetupUpdateReturnsNull();
        string name = new('h', trimmedLength);
        EditEventViewModel sut = CreateLoadedSut();
        sut.EventName = $"  {name}  ";

        await sut.SubmitCommand.ExecuteAsync(null);

        if (expectedSaved)
        {
            VerifyUpdateCalledOnceWithName(name);
            Assert.NotEqual(NameTooLongMessage, sut.ErrorMessage);
        }
        else
        {
            VerifyUpdateNeverCalled();
            Assert.Equal(expectedError, sut.ErrorMessage);
        }
    }

    // ── Request building ──────────────────────────────────────────────────────

    [Fact]
    public async Task SubmitCommand_SendsLoadedValuesForSameEvent()
    {
        EventDetail detail = CreateDetail();
        EditEventViewModel sut = CreateLoadedSut(detail);

        await sut.SubmitCommand.ExecuteAsync(null);

        Assert.NotNull(_sent);
        Assert.Equal("evt-800", _sent.Id);
        Assert.Equal("Cedar Grove Concert", _sent.EventName);
        Assert.Equal("Grove Amphitheatre", _sent.VenueName);
        Assert.Equal("confirmed", _sent.Status);
        Assert.Equal(detail.StartDateUtc, _sent.StartDateUtc);
        Assert.Equal(detail.EndDateUtc, _sent.EndDateUtc);
        Assert.Equal(900, _sent.ExpectedAttendees);
        Assert.Equal(0, _sent.AttendeeCount);
        Assert.Equal("A fictional summer concert.", _sent.ShortDescription);
        Assert.Equal("5 Example Grove", _sent.Location?.FormattedAddress);
        Assert.Equal("Grove Society", _sent.Organizer?.Name);
        Assert.Equal("https://grove.example.invalid", _sent.Organizer?.WebsiteUrl);
        Assert.Equal(["music", "summer"], _sent.Tags);
        Assert.Equal("Tickets", Assert.Single(_sent.Links!).Label);
        Assert.Equal("Stage", Assert.Single(_sent.Images!).AltText);
    }

    [Fact]
    public async Task SubmitCommand_SendsNullOptionalSections_WhenCleared()
    {
        EditEventViewModel sut = CreateLoadedSut();
        sut.ExpectedAttendeesText = string.Empty;
        sut.ShortDescription = " ";
        sut.LocationAddress = string.Empty;
        sut.OrganizerName = " ";
        sut.TagsText = " , ";
        sut.Links[0].Url = " ";
        sut.Images[0].Url = string.Empty;

        await sut.SubmitCommand.ExecuteAsync(null);

        Assert.NotNull(_sent);
        Assert.Equal(0, _sent.ExpectedAttendees);
        Assert.Null(_sent.ShortDescription);
        Assert.Null(_sent.Location);
        Assert.Null(_sent.Organizer);
        Assert.Null(_sent.Tags);
        Assert.Null(_sent.Links);
        Assert.Null(_sent.Images);
    }

    [Fact]
    public async Task SubmitCommand_TrimsValuesAndDropsLinksWithoutLabel()
    {
        EditEventViewModel sut = CreateLoadedSut();
        sut.EventName = "  Cedar Grove Concert II ";
        sut.OrganizerWebsite = "  ";
        sut.Links.Add(new LinkItem { Label = " ", Url = "https://nolabel.example.invalid" });
        sut.Links.Add(new LinkItem { Label = " Map ", Url = " https://maps.example.invalid/grove " });

        await sut.SubmitCommand.ExecuteAsync(null);

        Assert.NotNull(_sent);
        Assert.Equal("Cedar Grove Concert II", _sent.EventName);
        Assert.Null(_sent.Organizer?.WebsiteUrl);
        Assert.Equal(["Tickets", "Map"], _sent.Links!.Select(l => l.Label));
        Assert.Equal("https://maps.example.invalid/grove", _sent.Links![1].Url);
    }

    // ── Outcomes ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task SubmitCommand_ClosesPopupWithSuccess_WhenEventIsUpdated()
    {
        EditEventViewModel sut = CreateLoadedSut();
        sut.ErrorMessage = "stale";

        await sut.SubmitCommand.ExecuteAsync(null);

        Assert.False(sut.HasError);
        Assert.False(sut.IsBusy);
        VerifyPopupClosed(true, Times.Once());
    }

    [Fact]
    public async Task SubmitCommand_ShowsError_WhenServiceReturnsNull()
    {
        _eventsMock
            .Setup(e => e.UpdateEventAsync(It.IsAny<UpdateEventRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EventDetail?)null);
        EditEventViewModel sut = CreateLoadedSut();

        await sut.SubmitCommand.ExecuteAsync(null);

        Assert.Equal("Something went wrong. Please try again.", sut.ErrorMessage);
        Assert.False(sut.IsBusy);
        VerifyPopupClosed(true, Times.Never());
    }

    [Fact]
    public async Task SubmitCommand_ShowsError_WhenServiceThrows()
    {
        _eventsMock
            .Setup(e => e.UpdateEventAsync(It.IsAny<UpdateEventRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("offline"));
        EditEventViewModel sut = CreateLoadedSut();

        await sut.SubmitCommand.ExecuteAsync(null);

        Assert.Equal("Something went wrong. Please try again.", sut.ErrorMessage);
        Assert.False(sut.IsBusy);
    }

    [Fact]
    public async Task SubmitCommand_CanExecuteTracksBusyState()
    {
        TaskCompletionSource<EventDetail?> pending = new();
        _eventsMock
            .Setup(e => e.UpdateEventAsync(It.IsAny<UpdateEventRequest>(), It.IsAny<CancellationToken>()))
            .Returns(pending.Task);
        EditEventViewModel sut = CreateLoadedSut();

        Task submit = sut.SubmitCommand.ExecuteAsync(null);

        Assert.False(sut.SubmitCommand.CanExecute(null));
        pending.SetResult(null);
        await submit;
        Assert.True(sut.SubmitCommand.CanExecute(null));
    }

    [Fact]
    public async Task CancelCommand_ClosesPopupWithoutResult()
    {
        await CreateSut().CancelCommand.ExecuteAsync(null);

        VerifyPopupClosed(false, Times.Once());
    }
}
