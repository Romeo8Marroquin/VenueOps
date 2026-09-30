using Moq;
using VenueOps.Models;
using VenueOps.Models.Common;
using VenueOps.Models.Dashboard;
using VenueOps.Models.Events;
using VenueOps.Services;
using VenueOps.ViewModels;

namespace VenueOps.Tests.ViewModels;

public class DashboardViewModelTests
{
    private readonly Mock<IDashboardService> _dashboardMock = new();
    private readonly Mock<ISessionService> _sessionMock = new();
    private readonly Mock<INavigationService> _navMock = new();

    private readonly List<(int Page, int PageSize, string? Query, string? Status)> _recentCalls = [];

    public DashboardViewModelTests()
    {
        _dashboardMock
            .Setup(d => d.GetRecentEventsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback<int, int, string?, string?, CancellationToken>((p, s, q, st, _) => _recentCalls.Add((p, s, q, st)))
            .ReturnsAsync(new PagedResult<RecentEvent>
            {
                Items = [new RecentEvent { Id = "evt-100", EventName = "Riverside Fair" }],
                Page = 1,
                Total = 1,
                PageSize = 5
            });
        _dashboardMock.Setup(d => d.GetOverviewAsync(It.IsAny<CancellationToken>())).ReturnsAsync(CreateOverview());
        _navMock.Setup(n => n.PushCreateEventModalAsync(It.IsAny<Func<Task>>())).Returns(Task.CompletedTask);
        _navMock.Setup(n => n.GoToEventDetailAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
    }

    private DashboardViewModel CreateSut() => new(_dashboardMock.Object, _sessionMock.Object, _navMock.Object);

    private static DashboardOverview CreateOverview(string message = "Bookings are up.", string occupancy = "78%") => new()
    {
        Insight = new InsightInfo { Message = message, LastUpdatedUtc = new DateTime(2030, 3, 1, 9, 0, 0, DateTimeKind.Utc) },
        Stats = new DashboardStats
        {
            TotalEvents = 12,
            TotalBookings = 34,
            TotalAttendees = 560,
            ActiveVenues = 3,
            OccupancyRate = occupancy,
            Sponsors = ["Fictional Sponsor A", "Fictional Sponsor B", "Fictional Sponsor C"]
        }
    };

    // ── Initial state ─────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_SetsDefaults()
    {
        DashboardViewModel sut = CreateSut();

        Assert.Equal("Dashboard", sut.Title);
        Assert.Equal("All", sut.EventsStatusFilter);
        Assert.Equal("--", sut.OccupancyRate);
        Assert.False(sut.HasInsight);
        Assert.Equal(5, sut.RecentEvents.PageSize);
        Assert.Equal(["All", "Confirmed", "Draft", "Completed", "Cancelled"], sut.StatusFilters);
    }

    [Fact]
    public void IsWideLayout_TogglesCompactLayoutAndNotifies()
    {
        DashboardViewModel sut = CreateSut();
        List<string?> raised = [];
        sut.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        sut.IsWideLayout = true;

        Assert.False(sut.IsCompactLayout);
        Assert.Contains(nameof(DashboardViewModel.IsCompactLayout), raised);
    }

    // ── InitializeAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task InitializeAsync_LoadsWelcomeOverviewAndRecentEvents()
    {
        _sessionMock.Setup(s => s.CurrentUser).Returns(new UserInfo { Name = "  Lena Ortiz " });
        DashboardViewModel sut = CreateSut();

        await sut.InitializeAsync();

        Assert.Equal("Lena", sut.WelcomeName);
        Assert.Equal("Bookings are up.", sut.InsightMessage);
        Assert.Equal(new DateTime(2030, 3, 1, 9, 0, 0, DateTimeKind.Utc), sut.InsightUpdatedUtc);
        Assert.True(sut.HasInsight);
        Assert.Equal(12, sut.TotalEvents);
        Assert.Equal(34, sut.TotalBookings);
        Assert.Equal(560, sut.TotalAttendees);
        Assert.Equal(3, sut.ActiveVenues);
        Assert.Equal("78%", sut.OccupancyRate);
        Assert.Equal(3, sut.SponsorsCount);
        Assert.Equal([(1, 5, null, null)], _recentCalls);
        Assert.Equal("evt-100", Assert.Single(sut.RecentEvents.Items).Id);
        Assert.False(sut.IsBusy);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task InitializeAsync_UsesFriendlyFallback_WhenUserNameIsMissing(string? name)
    {
        _sessionMock.Setup(s => s.CurrentUser).Returns(name is null ? null : new UserInfo { Name = name });
        DashboardViewModel sut = CreateSut();

        await sut.InitializeAsync();

        Assert.Equal("there", sut.WelcomeName);
    }

    [Fact]
    public async Task InitializeAsync_UsesSingleWordNameAsIs()
    {
        _sessionMock.Setup(s => s.CurrentUser).Returns(new UserInfo { Name = "Mononym" });
        DashboardViewModel sut = CreateSut();

        await sut.InitializeAsync();

        Assert.Equal("Mononym", sut.WelcomeName);
    }

    [Fact]
    public async Task InitializeAsync_KeepsDefaults_WhenOverviewIsUnavailable()
    {
        _dashboardMock.Setup(d => d.GetOverviewAsync(It.IsAny<CancellationToken>())).ReturnsAsync((DashboardOverview?)null);
        DashboardViewModel sut = CreateSut();

        await sut.InitializeAsync();

        Assert.False(sut.HasInsight);
        Assert.Equal(string.Empty, sut.InsightMessage);
        Assert.Equal(0, sut.TotalEvents);
        Assert.Equal("--", sut.OccupancyRate);
        Assert.Single(sut.RecentEvents.Items);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task InitializeAsync_HidesInsightAndShowsPlaceholderRate_WhenValuesAreBlank(string blank)
    {
        _dashboardMock.Setup(d => d.GetOverviewAsync(It.IsAny<CancellationToken>())).ReturnsAsync(CreateOverview(blank, blank));
        DashboardViewModel sut = CreateSut();

        await sut.InitializeAsync();

        Assert.False(sut.HasInsight);
        Assert.Equal("--", sut.OccupancyRate);
    }

    [Fact]
    public async Task InitializeAsync_DoesNothing_WhenAlreadyBusy()
    {
        DashboardViewModel sut = CreateSut();
        sut.IsBusy = true;

        await sut.InitializeAsync();

        _dashboardMock.Verify(d => d.GetOverviewAsync(It.IsAny<CancellationToken>()), Times.Never);
        Assert.Empty(_recentCalls);
    }

    [Fact]
    public async Task InitializeAsync_ResetsBusy_WhenOverviewThrows()
    {
        _dashboardMock.Setup(d => d.GetOverviewAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new HttpRequestException("offline"));
        DashboardViewModel sut = CreateSut();

        await Assert.ThrowsAsync<HttpRequestException>(sut.InitializeAsync);

        Assert.False(sut.IsBusy);
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task RefreshAllCommand_ReloadsEverything()
    {
        DashboardViewModel sut = CreateSut();

        await sut.RefreshAllCommand.ExecuteAsync(null);

        _dashboardMock.Verify(d => d.GetOverviewAsync(It.IsAny<CancellationToken>()), Times.Once);
        Assert.Single(_recentCalls);
    }

    [Fact]
    public void RefreshAllCommand_CanExecuteTracksBusyState()
    {
        DashboardViewModel sut = CreateSut();
        int raised = 0;
        sut.RefreshAllCommand.CanExecuteChanged += (_, _) => raised++;
        Assert.True(sut.RefreshAllCommand.CanExecute(null));

        sut.IsBusy = true;

        Assert.False(sut.RefreshAllCommand.CanExecute(null));
        Assert.Equal(1, raised);
    }

    [Fact]
    public async Task CreateEventCommand_OpensModalWhoseCallbackReinitializes()
    {
        Func<Task>? onCreated = null;
        _navMock
            .Setup(n => n.PushCreateEventModalAsync(It.IsAny<Func<Task>>()))
            .Callback<Func<Task>>(cb => onCreated = cb)
            .Returns(Task.CompletedTask);
        DashboardViewModel sut = CreateSut();

        await sut.CreateEventCommand.ExecuteAsync(null);
        Assert.NotNull(onCreated);
        await onCreated();

        _dashboardMock.Verify(d => d.GetOverviewAsync(It.IsAny<CancellationToken>()), Times.Once);
        Assert.Single(_recentCalls);
    }

    [Fact]
    public async Task GoToEventDetailCommand_NavigatesWithEventId()
    {
        await CreateSut().GoToEventDetailCommand.ExecuteAsync(new RecentEvent { Id = "evt-321" });

        _navMock.Verify(n => n.GoToEventDetailAsync("evt-321"), Times.Once);
    }

    [Fact]
    public async Task SearchEventsCommand_PassesQueryAndLowercasedStatus()
    {
        DashboardViewModel sut = CreateSut();
        sut.EventsQuery = "fair";
        sut.EventsStatusFilter = "Completed"; // triggers its own load
        _recentCalls.Clear();

        await sut.SearchEventsCommand.ExecuteAsync(null);

        Assert.Equal([(1, 5, "fair", "completed")], _recentCalls);
    }

    [Fact]
    public async Task SearchEventsCommand_SendsNullQuery_WhenQueryIsWhitespace()
    {
        DashboardViewModel sut = CreateSut();
        sut.EventsQuery = "  ";

        await sut.SearchEventsCommand.ExecuteAsync(null);

        Assert.Null(Assert.Single(_recentCalls).Query);
    }

    [Fact]
    public void EventsStatusFilter_ChangeReloadsRecentEvents()
    {
        DashboardViewModel sut = CreateSut();

        sut.EventsStatusFilter = "Draft";

        Assert.Equal([(1, 5, null, "draft")], _recentCalls);
    }
}
