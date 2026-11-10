using System.Globalization;
using Moq;
using VenueOps.Models.Events;
using VenueOps.Services;
using VenueOps.ViewModels;

namespace VenueOps.Tests.ViewModels;

/// <summary>
/// QA plan cases for sharing an event from the event detail screen: the text format, the
/// enabled state across load phases, and failure handling.
/// </summary>
public class EventDetailShareTests
{
    private const string EventId = "evt-600";

    private readonly Mock<IEventsService> _eventsMock = new();
    private readonly Mock<INavigationService> _navMock = new();
    private readonly Mock<IShareService> _shareMock = new();

    private EventDetailViewModel CreateSut() => new(_eventsMock.Object, _navMock.Object, _shareMock.Object);

    private static EventDetail CreateDetail(string status = "confirmed") => new()
    {
        Id = EventId,
        EventName = "Aurora Night Market",
        VenueName = "Canal Yard",
        Status = status,
        StartDateUtc = new DateTime(2030, 5, 1, 9, 0, 0, DateTimeKind.Utc),
        EndDateUtc = new DateTime(2030, 5, 1, 11, 30, 0, DateTimeKind.Utc),
        ExpectedAtendees = 1200,
        AttendeeCount = 1180,
        BookingReference = "vno7k2x9q4",
        ShortDescription = "Fictional street food and music.",
        Images =
        [
            new EventImage { Url = "https://images.example.invalid/hero.jpg", Credit = "Studio One" },
            new EventImage { Url = "https://images.example.invalid/g1.jpg" }
        ],
        Location = new EventLocation { FormattedAddress = "3 Example Quay" },
        Organizer = new EventOrganizer { Name = "Night Markets Ltd", WebsiteUrl = "https://organizer.example.invalid" },
        Tags = ["food", "music"],
        Links = [new EventLink { Label = "Map", Url = "https://maps.example.invalid/canal-yard" }]
    };

    // Loads the detail through InitializeAsync, as the screen does.
    private async Task<EventDetailViewModel> LoadAsync(EventDetail detail)
    {
        _eventsMock
            .Setup(e => e.GetEventDetailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(detail);
        EventDetailViewModel sut = CreateSut();
        sut.ApplyQueryAttributes(new Dictionary<string, object> { ["id"] = EventId });
        await sut.InitializeAsync();
        return sut;
    }

    // Executes ShareCommand and returns the title and text handed to the share service.
    private async Task<(string Title, string Text)> ShareAsync(EventDetailViewModel sut)
    {
        string? title = null;
        string? text = null;
        _shareMock
            .Setup(s => s.ShareTextAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Callback<string, string>((t, x) => { title = t; text = x; })
            .Returns(Task.CompletedTask);

        await sut.ShareCommand.ExecuteAsync(null);

        return (title ?? throw new InvalidOperationException("ShareTextAsync was not called"),
                text ?? throw new InvalidOperationException("ShareTextAsync was not called"));
    }

    // ── AC1: share request ────────────────────────────────────────────────────

    [Fact]
    public async Task ShareCommand_RequestsShareSheetOnceWithEventNameAsTitleAndNonEmptyText()
    {
        EventDetailViewModel sut = await LoadAsync(CreateDetail());

        (string title, string text) = await ShareAsync(sut);

        Assert.Equal("Aurora Night Market", title);
        Assert.False(string.IsNullOrWhiteSpace(text));
        _shareMock.Verify(s => s.ShareTextAsync("Aurora Night Market", It.IsAny<string>()), Times.Once);
    }

    // ── AC1: plain text, no links ─────────────────────────────────────────────

    [Fact]
    public async Task ShareCommand_TextHasNoLinkOrUrl_WhenEventHoldsUrls()
    {
        EventDetail detail = CreateDetail(); // hero image, gallery, organizer site and link all hold URLs
        EventDetailViewModel sut = await LoadAsync(detail);

        (_, string text) = await ShareAsync(sut);

        Assert.Equal(5, text.Split('\n').Length);
        Assert.DoesNotContain("http", text);
        Assert.DoesNotContain("://", text);
        Assert.DoesNotContain("example.invalid", text);
    }

    // ── AC2: exact five-line format ───────────────────────────────────────────

    [Fact]
    public async Task ShareCommand_TextIsFiveLinesInRequiredOrder()
    {
        EventDetail detail = CreateDetail();
        detail.EventName = "Name";
        detail.VenueName = "Venue";
        detail.StartDateUtc = new DateTime(2030, 5, 1, 9, 0, 0, DateTimeKind.Utc);
        detail.EndDateUtc = new DateTime(2030, 5, 1, 11, 30, 0, DateTimeKind.Utc);
        detail.BookingReference = "BK-0500";
        EventDetailViewModel sut = await LoadAsync(detail);

        (_, string text) = await ShareAsync(sut);

        Assert.Equal("Name\nVenue\n2030-05-01T09:00:00Z\n2030-05-01T11:30:00Z\nBK-0500", text);
    }

    [Fact]
    public async Task ShareCommand_TextOmitsStatusDescriptionAttendeesAndOtherFields_ForCancelledEvent()
    {
        EventDetail detail = CreateDetail("cancelled");
        EventDetailViewModel sut = await LoadAsync(detail);

        (_, string text) = await ShareAsync(sut);

        Assert.Equal(5, text.Split('\n').Length);
        Assert.DoesNotContain("cancelled", text);
        Assert.DoesNotContain("Fictional street food and music.", text);
        Assert.DoesNotContain("food", text);
        Assert.DoesNotContain("1200", text);
        Assert.DoesNotContain("1180", text);
        Assert.DoesNotContain("Night Markets Ltd", text);
        Assert.DoesNotContain("3 Example Quay", text);
        Assert.DoesNotContain("Studio One", text);
    }

    [Fact]
    public async Task ShareCommand_WritesLocalStartAsUtc_WhenCultureIsGerman()
    {
        DateTime local = new(2030, 5, 1, 9, 0, 0, DateTimeKind.Local);
        EventDetail detail = CreateDetail();
        detail.StartDateUtc = local;
        EventDetailViewModel sut = await LoadAsync(detail);
        string expectedStart = local.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);

        CultureInfo original = CultureInfo.CurrentCulture;
        string text;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            (_, text) = await ShareAsync(sut);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }

        string start = text.Split('\n')[2];
        Assert.Matches(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}Z$", start);
        Assert.Equal(expectedStart, start);
    }

    [Fact]
    public async Task ShareCommand_WritesUnspecifiedEndAsGivenWithZSuffix()
    {
        EventDetail detail = CreateDetail();
        detail.EndDateUtc = new DateTime(2030, 5, 1, 11, 30, 0, DateTimeKind.Unspecified);
        EventDetailViewModel sut = await LoadAsync(detail);

        (_, string text) = await ShareAsync(sut);

        Assert.Equal("2030-05-01T11:30:00Z", text.Split('\n')[3]);
    }

    [Fact]
    public async Task ShareCommand_SharesBookingReferenceAsStored_NotGrouped()
    {
        EventDetail detail = CreateDetail();
        detail.BookingReference = "vno7k2x9q4";
        EventDetailViewModel sut = await LoadAsync(detail);

        (_, string text) = await ShareAsync(sut);

        string line = text.Split('\n')[4];
        Assert.Equal("vno7k2x9q4", line);
        Assert.NotEqual(sut.BookingReferenceDisplay, line);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task ShareCommand_StillGivesFiveLines_WhenBookingReferenceIsEmptyOrNull(string? reference)
    {
        EventDetail detail = CreateDetail();
        detail.BookingReference = reference!;
        EventDetailViewModel sut = await LoadAsync(detail);

        (_, string text) = await ShareAsync(sut);

        string[] lines = text.Split('\n');
        Assert.Equal(5, lines.Length);
        Assert.Equal(string.Empty, lines[4]);
    }

    [Fact]
    public async Task ShareCommand_TextStaysFiveLines_WhenEventNameContainsALineBreak()
    {
        EventDetail detail = CreateDetail();
        detail.EventName = "Line1\nLine2";
        EventDetailViewModel sut = await LoadAsync(detail);

        (_, string text) = await ShareAsync(sut);

        Assert.Equal(5, text.Split('\n').Length);
    }

    // ── AC3: enabled state ────────────────────────────────────────────────────

    [Theory]
    [InlineData("draft")]
    [InlineData("confirmed")]
    [InlineData("completed")]
    [InlineData("cancelled")]
    public async Task ShareCommand_IsEnabledAndShares_ForEveryStatus(string status)
    {
        EventDetailViewModel sut = await LoadAsync(CreateDetail(status));

        Assert.True(sut.ShareCommand.CanExecute(null));
        await sut.ShareCommand.ExecuteAsync(null);

        _shareMock.Verify(s => s.ShareTextAsync("Aurora Night Market", It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ShareCommand_IsDisabledAndNeverShares_WhenNoDetailIsLoaded()
    {
        EventDetailViewModel sut = CreateSut();

        Assert.Null(sut.Detail);
        Assert.False(sut.ShareCommand.CanExecute(null));
        await sut.ShareCommand.ExecuteAsync(null);

        _shareMock.Verify(s => s.ShareTextAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ShareCommand_IsDisabled_WhileDetailIsLoading()
    {
        var pending = new TaskCompletionSource<EventDetail?>(TaskCreationOptions.RunContinuationsAsynchronously);
        _eventsMock
            .Setup(e => e.GetEventDetailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(pending.Task);
        EventDetailViewModel sut = CreateSut();
        sut.ApplyQueryAttributes(new Dictionary<string, object> { ["id"] = EventId });

        Task load = sut.InitializeAsync();
        bool enabledWhileLoading = sut.ShareCommand.CanExecute(null);

        pending.SetResult(CreateDetail());
        await load;

        Assert.False(enabledWhileLoading);
        Assert.True(sut.ShareCommand.CanExecute(null));
    }

    [Fact]
    public async Task ShareCommand_IsDisabled_WhenLoadReturnsNull()
    {
        EventDetailViewModel sut = await LoadAsync(null!);

        Assert.True(sut.IsLoadError);
        Assert.False(sut.ShareCommand.CanExecute(null));
    }

    [Fact]
    public async Task ShareCommand_IsDisabled_WhenLoadThrows()
    {
        _eventsMock
            .Setup(e => e.GetEventDetailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("offline"));
        EventDetailViewModel sut = CreateSut();
        sut.ApplyQueryAttributes(new Dictionary<string, object> { ["id"] = EventId });

        await sut.InitializeAsync();

        Assert.True(sut.IsLoadError);
        Assert.False(sut.ShareCommand.CanExecute(null));
    }

    [Fact]
    public async Task ShareCommand_IsDisabledDuringReload_AndEnabledAgainAfterSuccess()
    {
        EventDetailViewModel sut = await LoadAsync(CreateDetail());
        var observed = new List<bool>();
        sut.ShareCommand.CanExecuteChanged += (_, _) => observed.Add(sut.ShareCommand.CanExecute(null));

        var pending = new TaskCompletionSource<EventDetail?>(TaskCreationOptions.RunContinuationsAsynchronously);
        _eventsMock
            .Setup(e => e.GetEventDetailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(pending.Task);

        Task reload = sut.InitializeAsync();
        bool enabledMidLoad = sut.ShareCommand.CanExecute(null);
        List<bool> transitionsMidLoad = [.. observed];

        pending.SetResult(CreateDetail());
        await reload;

        Assert.False(enabledMidLoad);
        Assert.Contains(false, transitionsMidLoad);
        Assert.True(sut.ShareCommand.CanExecute(null));
        Assert.Contains(true, observed);
        Assert.True(observed[^1]);
    }

    [Fact]
    public async Task ShareCommand_IsEnabledAfterRetry_FollowingAFailedLoad()
    {
        _eventsMock
            .Setup(e => e.GetEventDetailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("offline"));
        EventDetailViewModel sut = CreateSut();
        sut.ApplyQueryAttributes(new Dictionary<string, object> { ["id"] = EventId });
        await sut.InitializeAsync();
        bool enabledAfterFailure = sut.ShareCommand.CanExecute(null);

        _eventsMock
            .Setup(e => e.GetEventDetailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateDetail());
        await sut.RetryCommand.ExecuteAsync(null);

        Assert.False(enabledAfterFailure);
        Assert.True(sut.ShareCommand.CanExecute(null));
    }

    // ── AC4: nothing shown, failures swallowed ────────────────────────────────

    [Fact]
    public async Task ShareCommand_SuccessfulShare_ShowsNothingAndLeavesStateUnchanged()
    {
        EventDetail detail = CreateDetail();
        EventDetailViewModel sut = await LoadAsync(detail);
        _shareMock
            .Setup(s => s.ShareTextAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        var raised = new List<string?>();
        sut.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        await sut.ShareCommand.ExecuteAsync(null);

        _shareMock.Verify(s => s.ShareTextAsync("Aurora Night Market", It.IsAny<string>()), Times.Once);
        _navMock.VerifyNoOtherCalls();
        Assert.Empty(raised);
        Assert.Same(detail, sut.Detail);
        Assert.False(sut.IsLoadError);
        Assert.False(sut.IsBusy);
    }

    [Fact]
    public async Task ShareCommand_CatchesShareException_AndLeavesStateUnchanged()
    {
        _shareMock
            .Setup(s => s.ShareTextAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("share failed"));
        EventDetail detail = CreateDetail();
        EventDetailViewModel sut = await LoadAsync(detail);
        var raised = new List<string?>();
        sut.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        Exception? ex = await Record.ExceptionAsync(() => sut.ShareCommand.ExecuteAsync(null));

        Assert.Null(ex);
        Assert.Same(detail, sut.Detail);
        Assert.False(sut.IsLoadError);
        Assert.False(sut.IsBusy);
        Assert.Empty(raised);
        _navMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShareCommand_CatchesCancellation_AndStaysExecutableAfterRepeatedFailures()
    {
        _shareMock
            .Setup(s => s.ShareTextAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new OperationCanceledException());
        EventDetail detail = CreateDetail();
        EventDetailViewModel sut = await LoadAsync(detail);

        Exception? first = await Record.ExceptionAsync(() => sut.ShareCommand.ExecuteAsync(null));
        Exception? second = await Record.ExceptionAsync(() => sut.ShareCommand.ExecuteAsync(null));

        Assert.Null(first);
        Assert.Null(second);
        Assert.Same(detail, sut.Detail);
        Assert.False(sut.IsLoadError);
        Assert.False(sut.IsBusy);
        Assert.True(sut.ShareCommand.CanExecute(null));
        _shareMock.Verify(s => s.ShareTextAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(2));
    }
}
