using System.Globalization;
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
    private readonly Mock<IClipboardService> _clipboardMock = new();
    private readonly EventsListPreferencesService _preferences = new();

    /// <summary>Every text handed to <see cref="IClipboardService.SetTextAsync"/>, in call order.</summary>
    private readonly List<string> _copied = [];

    private record Call(
        int Page, int PageSize, string? Query, string? Status, string SortBy, string SortDirection,
        bool UpcomingOnly = false);

    private readonly List<Call> _calls = [];

    public EventsViewModelTests()
    {
        SetupGetEvents(_ => new PagedResult<RecentEvent>
        {
            Items = [new RecentEvent { Id = "evt-001", EventName = "Harbor Lights Gala" }],
            Page = 1,
            Total = 1,
            PageSize = 10
        });
        _navMock.Setup(n => n.PushCreateEventModalAsync(It.IsAny<Func<Task>>())).Returns(Task.CompletedTask);
        _navMock.Setup(n => n.GoToEventDetailAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
        _clipboardMock
            .Setup(c => c.SetTextAsync(It.IsAny<string>()))
            .Callback<string>(text => _copied.Add(text))
            .Returns(Task.CompletedTask);
    }

    /// <summary>Records every GetEventsAsync call and answers with the result built for the requested page.</summary>
    private void SetupGetEvents(Func<int, PagedResult<RecentEvent>> resultForPage)
    {
        _eventsMock
            .Setup(e => e.GetEventsAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Callback<int, int, string?, string?, string, string, bool, CancellationToken>(
                (page, size, query, status, sortBy, dir, upcomingOnly, _) =>
                    _calls.Add(new Call(page, size, query, status, sortBy, dir, upcomingOnly)))
            .Returns<int, int, string?, string?, string, string, bool, CancellationToken>(
                (page, _, _, _, _, _, _, _) => Task.FromResult<PagedResult<RecentEvent>?>(resultForPage(page)));
    }

    private EventsViewModel CreateSut()
        => new(_eventsMock.Object, _navMock.Object, _preferences, _clipboardMock.Object);

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
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
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

    // ── Upcoming only: default and request (AC1) ──────────────────────────────

    [Fact]
    public void UpcomingOnly_IsOff_ByDefault()
    {
        EventsViewModel sut = CreateSut();

        Assert.False(sut.UpcomingOnly);
        Assert.Empty(_calls);
    }

    [Fact]
    public async Task InitializeAsync_SendsUpcomingOnlyFalse_ByDefault()
    {
        EventsViewModel sut = CreateSut();

        await sut.InitializeAsync();

        Call call = Assert.Single(_calls);
        Assert.False(call.UpcomingOnly);
    }

    [Fact]
    public void UpcomingOnly_TurnedOn_ReloadsFirstPageWithUpcomingOnly()
    {
        EventsViewModel sut = CreateSut();

        sut.UpcomingOnly = true;

        Assert.True(sut.UpcomingOnly);
        Assert.Equal([new Call(1, 10, null, null, "startDateUtc", "desc", true)], _calls);
    }

    [Fact]
    public void UpcomingOnly_TurnedOn_DoesNotFilterTheItemsReturnedByTheService()
    {
        SetupGetEvents(_ => new PagedResult<RecentEvent>
        {
            Items =
            [
                new RecentEvent
                {
                    Id = "evt-002",
                    EventName = "Old Mill Market",
                    Status = "confirmed",
                    StartDateUtc = new DateTime(2020, 5, 1, 9, 0, 0, DateTimeKind.Utc),
                    EndDateUtc = new DateTime(2020, 5, 1, 17, 0, 0, DateTimeKind.Utc)
                }
            ],
            Page = 1,
            Total = 7,
            PageSize = 10,
            HasMore = true
        });
        EventsViewModel sut = CreateSut();

        sut.UpcomingOnly = true;

        Assert.Equal("evt-002", Assert.Single(sut.Events.Items).Id);
        Assert.Equal(7, sut.Events.TotalItems);
        Assert.True(sut.Events.HasMore);
    }

    [Fact]
    public void UpcomingOnly_TurnedBackOff_ReloadsWithUpcomingOnlyFalse()
    {
        EventsViewModel sut = CreateSut();
        sut.UpcomingOnly = true;
        _calls.Clear();

        sut.UpcomingOnly = false;

        Assert.False(sut.UpcomingOnly);
        Assert.Equal([new Call(1, 10, null, null, "startDateUtc", "desc", false)], _calls);
    }

    [Fact]
    public void UpcomingOnly_SetToItsCurrentValue_DoesNotReload()
    {
        EventsViewModel sut = CreateSut();
        List<string?> raised = [];
        sut.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        sut.UpcomingOnly = false;

        Assert.Empty(_calls);
        Assert.DoesNotContain(nameof(EventsViewModel.UpcomingOnly), raised);

        sut.UpcomingOnly = true;
        _calls.Clear();
        raised.Clear();

        sut.UpcomingOnly = true;

        Assert.Empty(_calls);
        Assert.Empty(raised);
    }

    [Fact]
    public void UpcomingOnly_RaisesPropertyChanged_WhenValueChanges()
    {
        EventsViewModel sut = CreateSut();
        List<string?> raised = [];
        sut.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        sut.UpcomingOnly = true;

        Assert.Contains(nameof(EventsViewModel.UpcomingOnly), raised);
    }

    // ── Upcoming only: remembered choice (AC2) ────────────────────────────────

    [Fact]
    public void UpcomingOnly_WhenToggled_IsWrittenToTheSharedPreferences()
    {
        EventsViewModel sut = CreateSut();

        sut.UpcomingOnly = true;
        Assert.True(_preferences.UpcomingOnly);

        sut.UpcomingOnly = false;
        Assert.False(_preferences.UpcomingOnly);
    }

    [Fact]
    public async Task NewViewModel_StartsWithTheChoiceMadeEarlier_AndSendsItOnItsFirstLoad()
    {
        EventsViewModel first = CreateSut();
        first.UpcomingOnly = true;
        _calls.Clear();

        EventsViewModel second = CreateSut();

        Assert.True(second.UpcomingOnly);
        Assert.Empty(_calls); // the constructor must not load

        await second.InitializeAsync();

        Call call = Assert.Single(_calls);
        Assert.Equal(1, call.Page);
        Assert.True(call.UpcomingOnly);
    }

    [Fact]
    public void NewViewModel_StartsWithUpcomingOnlyOff_WhenTheChoiceWasTurnedOffAgain()
    {
        EventsViewModel first = CreateSut();
        first.UpcomingOnly = true;
        first.UpcomingOnly = false;

        EventsViewModel second = CreateSut();

        Assert.False(second.UpcomingOnly);
    }

    [Fact]
    public void UpcomingOnly_InitialValueComesFromThePreferences_WithoutLoading()
    {
        _preferences.UpcomingOnly = true;

        EventsViewModel sut = CreateSut();

        Assert.True(sut.UpcomingOnly);
        Assert.Empty(_calls);
        Assert.Empty(sut.Events.Items);
    }

    // ── Upcoming only with search, status, sort and paging (AC3) ──────────────

    [Fact]
    public async Task UpcomingOnly_TurnedOn_KeepsQueryStatusAndSort()
    {
        EventsViewModel sut = CreateSut();
        sut.EventsQuery = "gala";
        sut.EventsStatusFilter = "Confirmed";
        sut.SortByFilter = "Venue";
        await sut.ToggleSortDirectionCommand.ExecuteAsync(null); // ascending
        _calls.Clear();

        sut.UpcomingOnly = true;

        Assert.Equal([new Call(1, 10, "gala", "confirmed", "venueName", "asc", true)], _calls);
    }

    [Fact]
    public async Task UpcomingOnly_StaysOn_WhenStatusFilterChanges()
    {
        EventsViewModel sut = CreateSut();
        sut.UpcomingOnly = true;
        _calls.Clear();

        sut.EventsStatusFilter = "Draft";

        Assert.Equal([new Call(1, 10, null, "draft", "startDateUtc", "desc", true)], _calls);
    }

    [Fact]
    public void UpcomingOnly_StaysOn_WhenSortFieldChanges()
    {
        EventsViewModel sut = CreateSut();
        sut.UpcomingOnly = true;
        _calls.Clear();

        sut.SortByFilter = "Event";

        Assert.Equal([new Call(1, 10, null, null, "eventName", "desc", true)], _calls);
    }

    [Fact]
    public async Task UpcomingOnly_StaysOn_WhenSortDirectionToggles()
    {
        EventsViewModel sut = CreateSut();
        sut.UpcomingOnly = true;
        _calls.Clear();

        await sut.ToggleSortDirectionCommand.ExecuteAsync(null);

        Assert.Equal([new Call(1, 10, null, null, "startDateUtc", "asc", true)], _calls);
    }

    [Fact]
    public async Task UpcomingOnly_StaysOn_WhenSearching()
    {
        EventsViewModel sut = CreateSut();
        sut.UpcomingOnly = true;
        sut.EventsQuery = "jazz";
        _calls.Clear();

        await sut.SearchEventsCommand.ExecuteAsync(null);

        Assert.Equal([new Call(1, 10, "jazz", null, "startDateUtc", "desc", true)], _calls);
    }

    [Fact]
    public async Task UpcomingOnly_StaysOn_WhenPagingAndRefreshing()
    {
        SetupGetEvents(page => new PagedResult<RecentEvent>
        {
            Items = [new RecentEvent { Id = $"evt-page-{page}" }],
            Page = page,
            Total = 30,
            PageSize = 10,
            HasMore = page < 3
        });
        EventsViewModel sut = CreateSut();
        sut.UpcomingOnly = true; // loads page 1
        _calls.Clear();

        await sut.Events.NextPageCommand.ExecuteAsync(null);

        Assert.Equal([new Call(2, 10, null, null, "startDateUtc", "desc", true)], _calls);
        Assert.Equal(2, sut.Events.CurrentPage);

        _calls.Clear();
        await sut.Events.RefreshCommand.ExecuteAsync(null);

        Assert.Equal([new Call(2, 10, null, null, "startDateUtc", "desc", true)], _calls);

        _calls.Clear();
        await sut.Events.PreviousPageCommand.ExecuteAsync(null);

        Assert.Equal([new Call(1, 10, null, null, "startDateUtc", "desc", true)], _calls);
    }

    [Fact]
    public async Task UpcomingOnly_Toggled_ReloadsFromFirstPage_WhenOnLaterPage()
    {
        SetupGetEvents(page => new PagedResult<RecentEvent>
        {
            Items = [new RecentEvent { Id = $"evt-page-{page}" }],
            Page = page,
            Total = 30,
            PageSize = 10,
            HasMore = page < 3
        });
        EventsViewModel sut = CreateSut();
        await sut.InitializeAsync();
        await sut.Events.NextPageCommand.ExecuteAsync(null);
        Assert.Equal(2, sut.Events.CurrentPage);
        _calls.Clear();

        sut.UpcomingOnly = true;

        Assert.Equal([new Call(1, 10, null, null, "startDateUtc", "desc", true)], _calls);
        Assert.Equal(1, sut.Events.CurrentPage);
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

    // ── Copy as CSV ───────────────────────────────────────────────────────────

    private const string CsvHeader = "Event,Venue,Status,Start (UTC),End (UTC),Attendees,Booking reference";
    private const string CrLf = "\r\n";

    private static readonly DateTime SampleStartUtc = new(2030, 3, 14, 18, 30, 0, DateTimeKind.Utc);
    private static readonly DateTime SampleEndUtc = new(2030, 3, 14, 23, 0, 0, DateTimeKind.Utc);

    private static RecentEvent MakeEvent(
        string? name = "Harbor Lights Gala",
        string? venue = "Pier Hall",
        string? status = "confirmed",
        DateTime? start = null,
        DateTime? end = null,
        int attendees = 10,
        string? reference = "BK-1001") => new()
    {
        Id = "evt-csv",
        EventName = name!,
        VenueName = venue!,
        Status = status!,
        StartDateUtc = start ?? SampleStartUtc,
        EndDateUtc = end ?? SampleEndUtc,
        AttendeeCount = attendees,
        BookingReference = reference!
    };

    private static PagedResult<RecentEvent> PageOf(params RecentEvent[] events) => new()
    {
        Items = [.. events],
        Page = 1,
        Total = events.Length,
        PageSize = 10
    };

    /// <summary>Loads a page holding the given events, runs the copy command, and returns the copied text.</summary>
    private async Task<string> LoadAndCopyAsync(params RecentEvent[] events)
    {
        SetupGetEvents(_ => PageOf(events));
        EventsViewModel sut = CreateSut();
        await sut.InitializeAsync();

        await sut.CopyAsCsvCommand.ExecuteAsync(null);

        return Assert.Single(_copied);
    }

    // T1 (AC1)
    [Fact]
    public async Task CopyAsCsvCommand_CopiesTheListedEventsOnce_WithoutRequestingEventsAgain()
    {
        SetupGetEvents(_ => PageOf(MakeEvent()));
        EventsViewModel sut = CreateSut();
        await sut.InitializeAsync();
        Assert.Single(_calls);

        await sut.CopyAsCsvCommand.ExecuteAsync(null);

        _clipboardMock.Verify(c => c.SetTextAsync(It.IsAny<string>()), Times.Once);
        Assert.Equal(
            CsvHeader + CrLf + "Harbor Lights Gala,Pier Hall,confirmed,2030-03-14T18:30:00Z,2030-03-14T23:00:00Z,10,BK-1001",
            Assert.Single(_copied));
        Assert.Single(_calls); // only the initial load reached the events service
    }

    // T2 (AC1)
    [Fact]
    public async Task CopyAsCsvCommand_CopiesOnlyTheEventsNowShown_AfterTheListReloads()
    {
        SetupGetEvents(_ => PageOf(MakeEvent(name: "Harbor Lights Gala"), MakeEvent(name: "Riverside Jazz Night")));
        EventsViewModel sut = CreateSut();
        await sut.InitializeAsync();
        SetupGetEvents(_ => PageOf(MakeEvent(name: "Old Mill Market", status: "draft")));

        sut.EventsStatusFilter = "Draft"; // reloads the list with the other page

        await sut.CopyAsCsvCommand.ExecuteAsync(null);

        string csv = Assert.Single(_copied);
        Assert.Equal(
            CsvHeader + CrLf + "Old Mill Market,Pier Hall,draft,2030-03-14T18:30:00Z,2030-03-14T23:00:00Z,10,BK-1001",
            csv);
        Assert.DoesNotContain("Harbor Lights Gala", csv);
        Assert.DoesNotContain("Riverside Jazz Night", csv);
    }

    // T3 (AC2)
    [Fact]
    public async Task CopyAsCsvCommand_WritesTheHeaderAndOneRowPerEvent_InListOrder()
    {
        string csv = await LoadAndCopyAsync(
            MakeEvent(
                name: "Riverside Jazz Night", venue: "Old Mill Theatre", status: "Confirmed",
                start: new DateTime(2030, 4, 2, 19, 0, 0, DateTimeKind.Utc),
                end: new DateTime(2030, 4, 2, 22, 15, 0, DateTimeKind.Utc),
                attendees: 85, reference: "BK-1002"),
            MakeEvent(
                name: "Harbor Lights Gala", venue: "Pier Hall", status: "cancelled",
                start: new DateTime(2030, 3, 14, 18, 30, 0, DateTimeKind.Utc),
                end: new DateTime(2030, 3, 14, 23, 0, 0, DateTimeKind.Utc),
                attendees: 250, reference: "BK-1001"));

        Assert.Equal(
            "Event,Venue,Status,Start (UTC),End (UTC),Attendees,Booking reference" + CrLf +
            "Riverside Jazz Night,Old Mill Theatre,Confirmed,2030-04-02T19:00:00Z,2030-04-02T22:15:00Z,85,BK-1002" + CrLf +
            "Harbor Lights Gala,Pier Hall,cancelled,2030-03-14T18:30:00Z,2030-03-14T23:00:00Z,250,BK-1001",
            csv);
    }

    // T4 (AC2)
    [Theory]
    [InlineData(DateTimeKind.Utc)]
    [InlineData(DateTimeKind.Unspecified)]
    [InlineData(DateTimeKind.Local)]
    public async Task CopyAsCsvCommand_WritesStartAndEndInUtcIso8601_WhateverTheDateKind(DateTimeKind kind)
    {
        DateTime startUtc = new(2030, 6, 1, 9, 15, 30, DateTimeKind.Utc);
        DateTime endUtc = startUtc.AddHours(2);
        (DateTime start, DateTime end) = kind switch
        {
            DateTimeKind.Local => (startUtc.ToLocalTime(), endUtc.ToLocalTime()),
            DateTimeKind.Unspecified => (
                DateTime.SpecifyKind(startUtc, DateTimeKind.Unspecified),
                DateTime.SpecifyKind(endUtc, DateTimeKind.Unspecified)),
            _ => (startUtc, endUtc)
        };
        Assert.Equal(kind, start.Kind);

        string csv = await LoadAndCopyAsync(MakeEvent(start: start, end: end));

        Assert.Equal(
            CsvHeader + CrLf + "Harbor Lights Gala,Pier Hall,confirmed,2030-06-01T09:15:30Z,2030-06-01T11:15:30Z,10,BK-1001",
            csv);
    }

    // T5 (AC3)
    [Theory]
    [InlineData("Plain Name", "Plain Name")]
    [InlineData("  padded  ", "  padded  ")]
    [InlineData("O'Brien's Fair", "O'Brien's Fair")]
    [InlineData("Gala, Spring Edition", "\"Gala, Spring Edition\"")]
    [InlineData("The \"Grand\" Opening", "\"The \"\"Grand\"\" Opening\"")]
    [InlineData("\"", "\"\"\"\"")]
    [InlineData("Line one\nLine two", "\"Line one\nLine two\"")]
    [InlineData("Line one\rLine two", "\"Line one\rLine two\"")]
    [InlineData("Line one\r\nLine two", "\"Line one\r\nLine two\"")]
    [InlineData("", "")]
    [InlineData(null, "")]
    public async Task CopyAsCsvCommand_QuotesAnEventNameOnlyWhenItNeedsIt(string? name, string expectedField)
    {
        string csv = await LoadAndCopyAsync(MakeEvent(name: name));

        Assert.Equal(
            CsvHeader + CrLf + expectedField + ",Pier Hall,confirmed,2030-03-14T18:30:00Z,2030-03-14T23:00:00Z,10,BK-1001",
            csv);
    }

    // T5 (AC3)
    [Fact]
    public async Task CopyAsCsvCommand_QuotesVenueStatusAndBookingReference_WhenTheyNeedIt()
    {
        string csv = await LoadAndCopyAsync(
            MakeEvent(venue: "Hall, East Wing", status: "needs \"review\"", reference: "BK\r\n9"));

        Assert.Equal(
            CsvHeader + CrLf +
            "Harbor Lights Gala,\"Hall, East Wing\",\"needs \"\"review\"\"\",2030-03-14T18:30:00Z,2030-03-14T23:00:00Z,10,\"BK\r\n9\"",
            csv);
    }

    // T5 (AC3)
    [Fact]
    public async Task CopyAsCsvCommand_WritesNullTextValuesAsEmptyFields()
    {
        string csv = await LoadAndCopyAsync(MakeEvent(venue: null, status: null, reference: null));

        Assert.Equal(
            CsvHeader + CrLf + "Harbor Lights Gala,,,2030-03-14T18:30:00Z,2030-03-14T23:00:00Z,10,",
            csv);
    }

    // T6 (AC3)
    [Theory]
    [InlineData("fr-FR")]
    [InlineData("th-TH")] // Buddhist calendar: a culture-sensitive date would show year 2573
    public async Task CopyAsCsvCommand_WritesTheSameDatesAndNumbers_WhateverTheCurrentCulture(string cultureName)
    {
        RecentEvent[] events =
        [
            MakeEvent(name: "Harbor Lights Gala", attendees: 1234),
            MakeEvent(name: "Riverside Jazz Night", attendees: 85, reference: "BK-1002")
        ];
        string invariantCsv = await RunCopyUnderCultureAsync(CultureInfo.InvariantCulture, events);
        _copied.Clear();

        string csv = await RunCopyUnderCultureAsync(CultureInfo.GetCultureInfo(cultureName), events);

        Assert.Equal(invariantCsv, csv);
        Assert.Equal(
            CsvHeader + CrLf +
            "Harbor Lights Gala,Pier Hall,confirmed,2030-03-14T18:30:00Z,2030-03-14T23:00:00Z,1234,BK-1001" + CrLf +
            "Riverside Jazz Night,Pier Hall,confirmed,2030-03-14T18:30:00Z,2030-03-14T23:00:00Z,85,BK-1002",
            csv);
    }

    // T6 (AC3)
    [Fact]
    public async Task CopyAsCsvCommand_SeparatesRowsWithCrLf_AndEndsWithoutALineBreak()
    {
        string csv = await LoadAndCopyAsync(
            MakeEvent(name: "Harbor Lights Gala"),
            MakeEvent(name: "Riverside Jazz Night"),
            MakeEvent(name: "Old Mill Market"));

        string[] lines = csv.Split(CrLf);
        Assert.Equal(4, lines.Length);
        Assert.Equal(CsvHeader, lines[0]);
        Assert.DoesNotContain("\n", csv.Replace(CrLf, string.Empty));
        Assert.DoesNotContain("\r", csv.Replace(CrLf, string.Empty));
        Assert.False(csv.EndsWith('\n'));
        Assert.False(csv.EndsWith('\r'));
    }

    private async Task<string> RunCopyUnderCultureAsync(CultureInfo culture, RecentEvent[] events)
    {
        CultureInfo original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = culture;
            return await LoadAndCopyAsync(events);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    // T7 (AC4)
    [Fact]
    public void CopyAsCsvCommand_CannotExecute_BeforeAnyLoad()
    {
        EventsViewModel sut = CreateSut();

        Assert.False(sut.CopyAsCsvCommand.CanExecute(null));
    }

    // T7 (AC4)
    [Fact]
    public async Task CopyAsCsvCommand_CannotExecute_WhenTheServiceReturnsAnEmptyPage()
    {
        SetupGetEvents(_ => PageOf());
        EventsViewModel sut = CreateSut();

        await sut.InitializeAsync();

        Assert.Empty(sut.Events.Items);
        Assert.False(sut.CopyAsCsvCommand.CanExecute(null));
    }

    // T7 (AC4)
    [Fact]
    public async Task CopyAsCsvCommand_CanExecute_OnceTheListHoldsAnEvent()
    {
        EventsViewModel sut = CreateSut();

        await sut.InitializeAsync();

        Assert.NotEmpty(sut.Events.Items);
        Assert.True(sut.CopyAsCsvCommand.CanExecute(null));
    }

    // T8 (AC4)
    [Fact]
    public async Task CopyAsCsvCommand_RaisesCanExecuteChanged_WhenALoadAddsEvents()
    {
        SetupGetEvents(_ => PageOf());
        EventsViewModel sut = CreateSut();
        await sut.InitializeAsync();
        Assert.False(sut.CopyAsCsvCommand.CanExecute(null));
        int raised = 0;
        sut.CopyAsCsvCommand.CanExecuteChanged += (_, _) => raised++;
        SetupGetEvents(_ => PageOf(MakeEvent()));

        await sut.Events.RefreshCommand.ExecuteAsync(null);

        Assert.True(raised > 0);
        Assert.True(sut.CopyAsCsvCommand.CanExecute(null));
    }

    // T8 (AC4)
    [Fact]
    public async Task CopyAsCsvCommand_BecomesNotExecutable_AfterAReloadThatReturnsNoEvents()
    {
        EventsViewModel sut = CreateSut();
        await sut.InitializeAsync();
        Assert.True(sut.CopyAsCsvCommand.CanExecute(null));
        int raised = 0;
        sut.CopyAsCsvCommand.CanExecuteChanged += (_, _) => raised++;
        SetupGetEvents(_ => PageOf());

        sut.EventsStatusFilter = "Cancelled"; // reloads: the list is cleared, then stays empty

        Assert.True(raised > 0);
        Assert.Empty(sut.Events.Items);
        Assert.False(sut.CopyAsCsvCommand.CanExecute(null));
    }
}
