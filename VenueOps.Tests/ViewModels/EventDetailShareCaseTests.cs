using Moq;
using VenueOps.Models.Events;
using VenueOps.Services;
using VenueOps.ViewModels;

namespace VenueOps.Tests.ViewModels;

/// <summary>
/// QA plan cases for sharing an event from the event detail screen that the other share tests
/// do not cover: the view model reaches the share service only, line breaks inside shared
/// fields, the enabled state across reloads, and share failures that happen late or while
/// the screen changes.
/// </summary>
public class EventDetailShareCaseTests
{
    private const string EventId = "evt-700";

    private readonly Mock<IEventsService> _eventsMock = new();
    private readonly Mock<INavigationService> _navMock = new();
    private readonly Mock<IShareService> _shareMock = new();

    private EventDetailViewModel CreateSut() => new(_eventsMock.Object, _navMock.Object, _shareMock.Object);

    private static EventDetail CreateDetail() => new()
    {
        Id = EventId,
        EventName = "Aurora Night Market",
        VenueName = "Canal Yard",
        Status = "confirmed",
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

    // Executes ShareCommand on the view model and returns the text handed to the share service.
    private async Task<string> ShareTextOfAsync(EventDetailViewModel sut)
    {
        string? captured = null;
        _shareMock
            .Setup(s => s.ShareTextAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Callback<string, string>((_, text) => captured = text)
            .Returns(Task.CompletedTask);

        await sut.ShareCommand.ExecuteAsync(null);

        // If building the text threw, ShareTextAsync is never called and this fails.
        return captured ?? throw new InvalidOperationException("ShareTextAsync was not called");
    }

    // Splits on every kind of line break, so a break left inside a field shows up as an extra line.
    private static string[] SplitLines(string text) => text.Split(["\r\n", "\n", "\r"], StringSplitOptions.None);

    // ── AC1: the view model uses only IShareService ───────────────────────────

    [Fact]
    public async Task ShareCommand_GoesThroughShareServiceOnly_ForLoadedEvent()
    {
        // Runs outside a MAUI app: if the view model reached Share.Default, this would throw.
        EventDetailViewModel sut = await LoadAsync(CreateDetail());
        _shareMock
            .Setup(s => s.ShareTextAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        await sut.ShareCommand.ExecuteAsync(null);

        _shareMock.Verify(s => s.ShareTextAsync("Aurora Night Market", It.IsAny<string>()), Times.Once);
        _shareMock.VerifyNoOtherCalls();
    }

    // ── AC2: line breaks inside shared fields ─────────────────────────────────

    [Theory]
    [InlineData("\r\n")]
    [InlineData("\r")]
    [InlineData("\n")]
    public async Task ShareCommand_ReplacesLineBreakInEventNameWithSingleSpace_AndKeepsFiveLines(string lineBreak)
    {
        EventDetail detail = CreateDetail();
        detail.EventName = "Aurora" + lineBreak + "Night Market";
        EventDetailViewModel sut = await LoadAsync(detail);

        string text = await ShareTextOfAsync(sut);

        string[] lines = SplitLines(text);
        Assert.Equal(5, lines.Length);
        Assert.Equal("Aurora Night Market", lines[0]);
    }

    [Fact]
    public async Task ShareCommand_ReplacesLineBreaksInVenueAndBookingReference_AndKeepsFiveLines()
    {
        EventDetail detail = CreateDetail();
        detail.VenueName = "Canal\nYard";
        detail.BookingReference = "BK\r\n0500";
        EventDetailViewModel sut = await LoadAsync(detail);

        string text = await ShareTextOfAsync(sut);

        string[] lines = SplitLines(text);
        Assert.Equal(5, lines.Length);
        Assert.Equal("Canal Yard", lines[1]);
        Assert.Equal("BK 0500", lines[4]);
    }

    [Fact]
    public async Task ShareCommand_EmptyVenueAndNullBookingReference_GiveFiveLinesWithEmptyFields_WithoutException()
    {
        EventDetail detail = CreateDetail();
        detail.VenueName = string.Empty;
        detail.BookingReference = null!;
        EventDetailViewModel sut = await LoadAsync(detail);

        string text = await ShareTextOfAsync(sut);

        string[] lines = text.Split('\n');
        Assert.Equal(5, lines.Length);
        Assert.Equal(string.Empty, lines[1]);
        Assert.Equal(string.Empty, lines[4]);
    }

    // ── AC3: enabled state across reloads ─────────────────────────────────────

    [Fact]
    public async Task ShareCommand_IsDisabledWhileReloading_AndEnabledAfterSuccessfulLoad_RaisingCanExecuteChanged()
    {
        EventDetailViewModel sut = await LoadAsync(CreateDetail());
        Assert.True(sut.ShareCommand.CanExecute(null));
        int raised = 0;
        sut.ShareCommand.CanExecuteChanged += (_, _) => raised++;

        var pending = new TaskCompletionSource<EventDetail?>(TaskCreationOptions.RunContinuationsAsynchronously);
        _eventsMock
            .Setup(e => e.GetEventDetailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(pending.Task);

        Task reload = sut.InitializeAsync();
        bool enabledWhileLoading = sut.ShareCommand.CanExecute(null);

        pending.SetResult(CreateDetail());
        await reload;

        Assert.False(enabledWhileLoading);
        Assert.True(sut.ShareCommand.CanExecute(null));
        Assert.True(raised > 0);
    }

    [Fact]
    public async Task ShareCommand_IsDisabledAfterReloadFails_FollowingAGoodLoad()
    {
        EventDetailViewModel sut = await LoadAsync(CreateDetail());
        Assert.True(sut.ShareCommand.CanExecute(null));
        _eventsMock
            .Setup(e => e.GetEventDetailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("offline"));

        await sut.InitializeAsync();

        Assert.True(sut.IsLoadError);
        Assert.False(sut.ShareCommand.CanExecute(null));
    }

    // ── AC4: failures are swallowed and nothing is shown ──────────────────────

    [Fact]
    public async Task ShareCommand_CatchesShareFailureThatFaultsAfterAnAwait_AndLeavesStateUnchanged()
    {
        EventDetail detail = CreateDetail();
        EventDetailViewModel sut = await LoadAsync(detail);
        _shareMock
            .Setup(s => s.ShareTextAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(() => FaultAfterYieldAsync());

        Exception? ex = await Record.ExceptionAsync(() => sut.ShareCommand.ExecuteAsync(null));

        Assert.Null(ex);
        Assert.Same(detail, sut.Detail);
        Assert.False(sut.IsLoadError);
        Assert.False(sut.IsBusy);
        _navMock.VerifyNoOtherCalls();
    }

    private static async Task FaultAfterYieldAsync()
    {
        await Task.Yield();
        throw new InvalidOperationException("share failed after an await");
    }

    [Fact]
    public async Task ShareCommand_ActivatedTwiceWhileShareIsPending_NeitherRunThrows_AndStateIsUnchanged()
    {
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int requests = 0;
        _shareMock
            .Setup(s => s.ShareTextAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Callback<string, string>((_, _) => requests++)
            .Returns(() => release.Task);
        EventDetail detail = CreateDetail();
        EventDetailViewModel sut = await LoadAsync(detail);

        Task first = sut.ShareCommand.ExecuteAsync(null);
        Task second = sut.ShareCommand.ExecuteAsync(null);
        release.SetResult();
        Exception? ex = await Record.ExceptionAsync(() => Task.WhenAll(first, second));

        // The criteria do not say whether the second activation is ignored or sent, so the
        // number of share requests is recorded and may be one or two.
        Assert.Null(ex);
        Assert.InRange(requests, 1, 2);
        Assert.Same(detail, sut.Detail);
        Assert.False(sut.IsLoadError);
        Assert.False(sut.IsBusy);
        _navMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShareCommand_DetailClearedWhileShareSheetIsOpen_RaisesNothingAndShowsNothing()
    {
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _shareMock
            .Setup(s => s.ShareTextAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(() => release.Task);
        EventDetailViewModel sut = await LoadAsync(CreateDetail());

        Task share = sut.ShareCommand.ExecuteAsync(null);
        sut.Detail = null;
        release.SetResult();
        Exception? ex = await Record.ExceptionAsync(() => share);

        // The state stays as the clearing left it: no detail, no load error, not busy.
        Assert.Null(ex);
        Assert.Null(sut.Detail);
        Assert.False(sut.IsLoadError);
        Assert.False(sut.IsBusy);
        _navMock.VerifyNoOtherCalls();
    }
}
