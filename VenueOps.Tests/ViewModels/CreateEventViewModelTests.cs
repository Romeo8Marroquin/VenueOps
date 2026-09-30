using CommunityToolkit.Maui;
using Moq;
using VenueOps.Models.Events;
using VenueOps.Services;
using VenueOps.ViewModels;

namespace VenueOps.Tests.ViewModels;

public class CreateEventViewModelTests
{
    private readonly Mock<IEventsService> _eventsMock = new();
    private readonly Mock<IPopupService> _popupMock = new();
    private CreateEventRequest? _sent;

    public CreateEventViewModelTests()
    {
        _eventsMock
            .Setup(e => e.CreateEventAsync(It.IsAny<CreateEventRequest>(), It.IsAny<CancellationToken>()))
            .Callback<CreateEventRequest, CancellationToken>((r, _) => _sent = r)
            .ReturnsAsync(new CreateEventResponse { Id = "evt-700", EventName = "Tidal Arts Fest", BookingReference = "BK-0700" });
    }

    private CreateEventViewModel CreateSut() => new(_eventsMock.Object, _popupMock.Object);

    private CreateEventViewModel CreateValidSut()
    {
        CreateEventViewModel sut = CreateSut();
        sut.EventName = "  Tidal Arts Fest ";
        sut.VenueName = " Harbour Hall ";
        return sut;
    }

    private void VerifyPopupClosed(bool result, Times times) =>
        _popupMock.Verify(p => p.ClosePopupAsync(It.IsAny<Page>(), result, It.IsAny<CancellationToken>()), times);

    // ── Initial state ─────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_SetsDefaults()
    {
        CreateEventViewModel sut = CreateSut();

        Assert.Equal("New Event", sut.Title);
        Assert.Equal("Draft", sut.Status);
        Assert.Equal(DateTime.Today, sut.StartDate);
        Assert.Equal(new TimeSpan(9, 0, 0), sut.StartTime);
        Assert.Equal(DateTime.Today, sut.EndDate);
        Assert.Equal(new TimeSpan(10, 0, 0), sut.EndTime);
        Assert.Empty(sut.Links);
        Assert.Empty(sut.Images);
        Assert.False(sut.HasError);
        Assert.True(sut.IsCompactLayout);
        Assert.Equal(["Draft", "Confirmed", "Cancelled"], sut.StatusOptions);
    }

    [Fact]
    public void IsWideLayout_TogglesCompactLayoutAndNotifies()
    {
        CreateEventViewModel sut = CreateSut();
        List<string?> raised = [];
        sut.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        sut.IsWideLayout = true;

        Assert.False(sut.IsCompactLayout);
        Assert.Contains(nameof(CreateEventViewModel.IsCompactLayout), raised);
    }

    // ── Collections ───────────────────────────────────────────────────────────

    [Fact]
    public void LinkCommands_AddAndRemoveItems()
    {
        CreateEventViewModel sut = CreateSut();

        sut.AddLinkCommand.Execute(null);
        sut.AddLinkCommand.Execute(null);
        LinkItem first = sut.Links[0];
        sut.RemoveLinkCommand.Execute(first);

        LinkItem remaining = Assert.Single(sut.Links);
        Assert.NotSame(first, remaining);
        Assert.Equal(string.Empty, remaining.Label);
        Assert.Equal(string.Empty, remaining.Url);
    }

    [Fact]
    public void ImageCommands_AddAndRemoveItems()
    {
        CreateEventViewModel sut = CreateSut();

        sut.AddImageCommand.Execute(null);
        ImageItem added = Assert.Single(sut.Images);
        Assert.Equal(string.Empty, added.Url);
        Assert.Equal(string.Empty, added.AltText);
        Assert.Equal(string.Empty, added.Credit);
        sut.RemoveImageCommand.Execute(added);

        Assert.Empty(sut.Images);
    }

    // ── Validation ────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("", "Harbour Hall", "Event name is required.")]
    [InlineData("   ", "Harbour Hall", "Event name is required.")]
    [InlineData("Tidal Arts Fest", "", "Venue name is required.")]
    [InlineData("Tidal Arts Fest", "  ", "Venue name is required.")]
    public async Task SubmitCommand_RequiresNames(string eventName, string venueName, string expectedError)
    {
        CreateEventViewModel sut = CreateSut();
        sut.EventName = eventName;
        sut.VenueName = venueName;

        await sut.SubmitCommand.ExecuteAsync(null);

        Assert.Equal(expectedError, sut.ErrorMessage);
        Assert.True(sut.HasError);
        _eventsMock.Verify(e => e.CreateEventAsync(It.IsAny<CreateEventRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(10, 9)]  // end before start
    [InlineData(9, 9)]   // end equal to start
    public async Task SubmitCommand_RequiresEndAfterStart(int startHour, int endHour)
    {
        CreateEventViewModel sut = CreateValidSut();
        sut.StartTime = TimeSpan.FromHours(startHour);
        sut.EndTime = TimeSpan.FromHours(endHour);

        await sut.SubmitCommand.ExecuteAsync(null);

        Assert.Equal("End date/time must be after start date/time.", sut.ErrorMessage);
    }

    [Fact]
    public async Task SubmitCommand_AcceptsEarlierTime_OnLaterEndDate()
    {
        CreateEventViewModel sut = CreateValidSut();
        sut.EndDate = sut.StartDate.AddDays(1);
        sut.EndTime = TimeSpan.FromHours(8);

        await sut.SubmitCommand.ExecuteAsync(null);

        Assert.False(sut.HasError);
        Assert.NotNull(_sent);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("-1")]
    [InlineData("12.5")]
    public async Task SubmitCommand_RejectsInvalidAttendeeCount(string attendees)
    {
        CreateEventViewModel sut = CreateValidSut();
        sut.ExpectedAttendeesText = attendees;

        await sut.SubmitCommand.ExecuteAsync(null);

        Assert.Equal("Expected attendees must be a non-negative number.", sut.ErrorMessage);
        _eventsMock.Verify(e => e.CreateEventAsync(It.IsAny<CreateEventRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Request building ──────────────────────────────────────────────────────

    [Fact]
    public async Task SubmitCommand_SendsMinimalRequest_WhenOptionalFieldsAreBlank()
    {
        CreateEventViewModel sut = CreateValidSut();

        await sut.SubmitCommand.ExecuteAsync(null);

        Assert.NotNull(_sent);
        Assert.Equal("Tidal Arts Fest", _sent.EventName);
        Assert.Equal("Harbour Hall", _sent.VenueName);
        Assert.Equal("draft", _sent.Status);
        Assert.Equal((DateTime.Today + new TimeSpan(9, 0, 0)).ToUniversalTime(), _sent.StartDateUtc);
        Assert.Equal((DateTime.Today + new TimeSpan(10, 0, 0)).ToUniversalTime(), _sent.EndDateUtc);
        Assert.Equal(0, _sent.ExpectedAtendees);
        Assert.Null(_sent.ShortDescription);
        Assert.Null(_sent.Location);
        Assert.Null(_sent.Organizer);
        Assert.Null(_sent.Tags);
        Assert.Null(_sent.Links);
        Assert.Null(_sent.Images);
    }

    [Fact]
    public async Task SubmitCommand_SendsTrimmedOptionalFieldsAndFiltersIncompleteItems()
    {
        CreateEventViewModel sut = CreateValidSut();
        sut.Status = "Confirmed";
        sut.ExpectedAttendeesText = "250";
        sut.ShortDescription = "  A fictional arts weekend. ";
        sut.LocationAddress = " 4 Example Pier ";
        sut.OrganizerName = " Tidal Collective ";
        sut.OrganizerWebsite = " https://tidal.example.invalid ";
        sut.TagsText = " art, , music ,outdoor,";
        sut.Links.Add(new LinkItem { Label = " Tickets ", Url = " https://tickets.example.invalid/700 " });
        sut.Links.Add(new LinkItem { Label = "No url", Url = "  " });
        sut.Links.Add(new LinkItem { Label = " ", Url = "https://nolabel.example.invalid" });
        sut.Images.Add(new ImageItem { Url = " https://images.example.invalid/700.jpg ", AltText = " Stage ", Credit = " Studio " });
        sut.Images.Add(new ImageItem { Url = " ", AltText = "Missing url" });

        await sut.SubmitCommand.ExecuteAsync(null);

        Assert.NotNull(_sent);
        Assert.Equal("confirmed", _sent.Status);
        Assert.Equal(250, _sent.ExpectedAtendees);
        Assert.Equal("A fictional arts weekend.", _sent.ShortDescription);
        Assert.Equal("4 Example Pier", _sent.Location?.FormattedAddress);
        Assert.Equal("Tidal Collective", _sent.Organizer?.Name);
        Assert.Equal("https://tidal.example.invalid", _sent.Organizer?.WebsiteUrl);
        Assert.Equal(["art", "music", "outdoor"], _sent.Tags);
        EventLink link = Assert.Single(_sent.Links!);
        Assert.Equal("Tickets", link.Label);
        Assert.Equal("https://tickets.example.invalid/700", link.Url);
        EventImage image = Assert.Single(_sent.Images!);
        Assert.Equal("https://images.example.invalid/700.jpg", image.Url);
        Assert.Equal("Stage", image.AltText);
        Assert.Equal("Studio", image.Credit);
    }

    [Fact]
    public async Task SubmitCommand_OmitsOrganizerWebsite_WhenBlank()
    {
        CreateEventViewModel sut = CreateValidSut();
        sut.OrganizerName = "Tidal Collective";
        sut.OrganizerWebsite = "  ";

        await sut.SubmitCommand.ExecuteAsync(null);

        Assert.NotNull(_sent?.Organizer);
        Assert.Null(_sent.Organizer.WebsiteUrl);
    }

    // ── Outcomes ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task SubmitCommand_ClosesPopupWithSuccess_WhenEventIsCreated()
    {
        CreateEventViewModel sut = CreateValidSut();
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
            .Setup(e => e.CreateEventAsync(It.IsAny<CreateEventRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CreateEventResponse?)null);
        CreateEventViewModel sut = CreateValidSut();

        await sut.SubmitCommand.ExecuteAsync(null);

        Assert.Equal("Something went wrong. Please try again.", sut.ErrorMessage);
        Assert.False(sut.IsBusy);
        VerifyPopupClosed(true, Times.Never());
    }

    [Fact]
    public async Task SubmitCommand_ShowsError_WhenServiceThrows()
    {
        _eventsMock
            .Setup(e => e.CreateEventAsync(It.IsAny<CreateEventRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("offline"));
        CreateEventViewModel sut = CreateValidSut();

        await sut.SubmitCommand.ExecuteAsync(null);

        Assert.Equal("Something went wrong. Please try again.", sut.ErrorMessage);
        Assert.False(sut.IsBusy);
        VerifyPopupClosed(true, Times.Never());
    }

    [Fact]
    public async Task SubmitCommand_CanExecuteTracksBusyState()
    {
        TaskCompletionSource<CreateEventResponse?> pending = new();
        _eventsMock
            .Setup(e => e.CreateEventAsync(It.IsAny<CreateEventRequest>(), It.IsAny<CancellationToken>()))
            .Returns(pending.Task);
        CreateEventViewModel sut = CreateValidSut();
        Assert.True(sut.SubmitCommand.CanExecute(null));

        Task submit = sut.SubmitCommand.ExecuteAsync(null);

        Assert.True(sut.IsBusy);
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

    // ── Item types ────────────────────────────────────────────────────────────

    [Fact]
    public void ItemTypes_RaisePropertyChanged()
    {
        LinkItem link = new();
        ImageItem image = new();
        List<string?> raised = [];
        link.PropertyChanged += (_, e) => raised.Add(e.PropertyName);
        image.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        link.Label = "Map";
        link.Url = "https://maps.example.invalid";
        image.Url = "https://images.example.invalid/a.jpg";
        image.AltText = "Alt";
        image.Credit = "Credit";

        Assert.Equal(["Label", "Url", "Url", "AltText", "Credit"], raised);
    }
}
