using Moq;
using VenueOps.Models.Common;
using VenueOps.Models.Events;
using VenueOps.Services;
using VenueOps.ViewModels;

namespace VenueOps.Tests.ViewModels;

public class EventsViewModelTests
{
    private readonly Mock<IEventsService> _eventsMock = new();
    private readonly Mock<INavigationService> _navMock = new();

    private record Call(int Page, int PageSize, string? Query, string? Status, string SortBy, string SortDirection);

    private readonly List<Call> _calls = [];

    public EventsViewModelTests()
    {
        _eventsMock
            .Setup(e => e.GetEventsAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<int, int, string?, string?, string, string, CancellationToken>(
                (page, size, query, status, sortBy, dir, _) => _calls.Add(new Call(page, size, query, status, sortBy, dir)))
            .ReturnsAsync(new PagedResult<RecentEvent>
            {
                Items = [new RecentEvent { Id = "evt-001", EventName = "Harbor Lights Gala" }],
                Page = 1,
                Total = 1,
                PageSize = 10
            });
        _navMock.Setup(n => n.PushCreateEventModalAsync(It.IsAny<Func<Task>>())).Returns(Task.CompletedTask);
        _navMock.Setup(n => n.GoToEventDetailAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
    }

    private EventsViewModel CreateSut() => new(_eventsMock.Object, _navMock.Object);

    // ── Initial state ─────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_SetsDefaults()
    {
        EventsViewModel sut = CreateSut();

        Assert.Equal("Event List", sut.Title);
        Assert.Equal(string.Empty, sut.EventsQuery);
        Assert.Equal("All", sut.EventsStatusFilter);
        Assert.Equal("Date", sut.SortByFilter);
        Assert.False(sut.SortAscending);
        Assert.Equal("↓", sut.SortDirectionSymbol);
        Assert.Equal(10, sut.Events.PageSize);
        Assert.Equal(["All", "Confirmed", "Draft", "Completed", "Cancelled"], sut.StatusFilters);
        Assert.Equal(["Date", "Event", "Venue", "Attendees", "Booking Ref", "Status"], sut.SortByFields);
    }

    [Fact]
    public void IsWideLayout_TogglesCompactLayoutAndNotifies()
    {
        EventsViewModel sut = CreateSut();
        List<string?> raised = [];
        sut.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        sut.IsWideLayout = true;

        Assert.False(sut.IsCompactLayout);
        Assert.Contains(nameof(EventsViewModel.IsCompactLayout), raised);
    }

    // ── InitializeAsync / fetch arguments ─────────────────────────────────────

    [Fact]
    public async Task InitializeAsync_LoadsFirstPageWithDefaultFilters()
    {
        EventsViewModel sut = CreateSut();

        await sut.InitializeAsync();

        Assert.Equal([new Call(1, 10, null, null, "startDateUtc", "desc")], _calls);
        Assert.Equal("evt-001", Assert.Single(sut.Events.Items).Id);
        Assert.False(sut.IsBusy);
    }

    [Fact]
    public async Task InitializeAsync_DoesNothing_WhenAlreadyBusy()
    {
        EventsViewModel sut = CreateSut();
        sut.IsBusy = true;

        await sut.InitializeAsync();

        Assert.Empty(_calls);
    }

    [Fact]
    public async Task InitializeAsync_ResetsBusy_WhenLoadThrows()
    {
        _eventsMock
            .Setup(e => e.GetEventsAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("offline"));
        EventsViewModel sut = CreateSut();

        await Assert.ThrowsAsync<HttpRequestException>(sut.InitializeAsync);

        Assert.False(sut.IsBusy);
    }

    [Fact]
    public async Task SearchEventsCommand_PassesQueryAndLowercasedStatus()
    {
        EventsViewModel sut = CreateSut();
        sut.EventsQuery = "gala";
        sut.EventsStatusFilter = "Confirmed"; // triggers its own load
        _calls.Clear();

        await sut.SearchEventsCommand.ExecuteAsync(null);

        Call call = Assert.Single(_calls);
        Assert.Equal("gala", call.Query);
        Assert.Equal("confirmed", call.Status);
    }

    [Fact]
    public async Task SearchEventsCommand_SendsNullQuery_WhenQueryIsWhitespace()
    {
        EventsViewModel sut = CreateSut();
        sut.EventsQuery = "   ";

        await sut.SearchEventsCommand.ExecuteAsync(null);

        Assert.Null(Assert.Single(_calls).Query);
    }

    [Theory]
    [InlineData("Event", "eventName")]
    [InlineData("Venue", "venueName")]
    [InlineData("Attendees", "attendeeCount")]
    [InlineData("Booking Ref", "bookingReference")]
    [InlineData("Status", "status")]
    [InlineData("Unknown Column", "startDateUtc")]
    public void SortByFilter_ChangeReloadsWithMappedApiField(string label, string expectedField)
    {
        EventsViewModel sut = CreateSut();

        sut.SortByFilter = label;

        Assert.Equal(expectedField, Assert.Single(_calls).SortBy);
    }

    [Fact]
    public void SortByFilter_SwitchingBackToDate_SortsByStartDate()
    {
        EventsViewModel sut = CreateSut();
        sut.SortByFilter = "Event";
        _calls.Clear();

        sut.SortByFilter = "Date";

        Assert.Equal("startDateUtc", Assert.Single(_calls).SortBy);
    }

    [Fact]
    public void EventsStatusFilter_ChangeReloadsFirstPage()
    {
        EventsViewModel sut = CreateSut();

        sut.EventsStatusFilter = "Draft";

        Call call = Assert.Single(_calls);
        Assert.Equal(1, call.Page);
        Assert.Equal("draft", call.Status);
    }

    [Fact]
    public async Task ToggleSortDirectionCommand_FlipsDirectionAndReloads()
    {
        EventsViewModel sut = CreateSut();
        List<string?> raised = [];
        sut.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        await sut.ToggleSortDirectionCommand.ExecuteAsync(null);

        Assert.True(sut.SortAscending);
        Assert.Equal("↑", sut.SortDirectionSymbol);
        Assert.Contains(nameof(EventsViewModel.SortDirectionSymbol), raised);
        Assert.Equal("asc", Assert.Single(_calls).SortDirection);

        await sut.ToggleSortDirectionCommand.ExecuteAsync(null);

        Assert.False(sut.SortAscending);
        Assert.Equal("desc", _calls[^1].SortDirection);
    }

    // ── Query attributes ──────────────────────────────────────────────────────

    [Fact]
    public void ApplyQueryAttributes_SetsUnescapedQuery()
    {
        EventsViewModel sut = CreateSut();

        sut.ApplyQueryAttributes(new Dictionary<string, object> { ["query"] = "jazz%20night" });

        Assert.Equal("jazz night", sut.EventsQuery);
    }

    public static TheoryData<Dictionary<string, object>> IgnoredQueryCases => new()
    {
        new Dictionary<string, object>(),
        new Dictionary<string, object> { ["query"] = string.Empty },
        new Dictionary<string, object> { ["query"] = 42 },
        new Dictionary<string, object> { ["other"] = "value" }
    };

    [Theory]
    [MemberData(nameof(IgnoredQueryCases))]
    public void ApplyQueryAttributes_KeepsQuery_WhenValueIsMissingEmptyOrNotAString(Dictionary<string, object> query)
    {
        EventsViewModel sut = CreateSut();
        sut.EventsQuery = "existing";

        sut.ApplyQueryAttributes(query);

        Assert.Equal("existing", sut.EventsQuery);
    }

    // ── Navigation ────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateEventCommand_OpensModalWhoseCallbackReloadsEvents()
    {
        Func<Task>? onCreated = null;
        _navMock
            .Setup(n => n.PushCreateEventModalAsync(It.IsAny<Func<Task>>()))
            .Callback<Func<Task>>(cb => onCreated = cb)
            .Returns(Task.CompletedTask);
        EventsViewModel sut = CreateSut();

        await sut.CreateEventCommand.ExecuteAsync(null);
        Assert.NotNull(onCreated);
        Assert.Empty(_calls);
        await onCreated();

        Assert.Single(_calls);
    }

    [Fact]
    public async Task GoToEventDetailCommand_NavigatesWithEventId()
    {
        await CreateSut().GoToEventDetailCommand.ExecuteAsync(new RecentEvent { Id = "evt-042" });

        _navMock.Verify(n => n.GoToEventDetailAsync("evt-042"), Times.Once);
    }
}
