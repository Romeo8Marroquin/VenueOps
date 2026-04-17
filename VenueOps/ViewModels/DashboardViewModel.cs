using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VenueOps.Models.Events;
using VenueOps.Services;
using VenueOps.ViewModels.Common;

namespace VenueOps.ViewModels;

public partial class DashboardViewModel : BaseViewModel
{
    private readonly IDashboardService _dashboardService;
    private readonly ISessionService   _sessionService;

    // ── Welcome ───────────────────────────────────────────────────────────
    [ObservableProperty]
    public partial string WelcomeName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SearchQuery { get; set; } = string.Empty;

    // ── Insight ───────────────────────────────────────────────────────────
    [ObservableProperty]
    public partial string InsightMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial DateTime InsightUpdatedUtc { get; set; }

    [ObservableProperty]
    public partial bool HasInsight { get; set; }

    // ── Stats ─────────────────────────────────────────────────────────────
    [ObservableProperty]
    public partial int TotalEvents { get; set; }

    [ObservableProperty]
    public partial int TotalBookings { get; set; }

    [ObservableProperty]
    public partial int TotalAttendees { get; set; }

    [ObservableProperty]
    public partial int ActiveVenues { get; set; }

    [ObservableProperty]
    public partial string OccupancyRate { get; set; } = "--";

    [ObservableProperty]
    public partial int SponsorsCount { get; set; }

    // ── Recent Events (paginated) ─────────────────────────────────────────
    public PaginatedSection<RecentEvent> RecentEvents { get; }

    public DashboardViewModel(IDashboardService dashboardService, ISessionService sessionService)
    {
        _dashboardService = dashboardService;
        _sessionService   = sessionService;
        Title = "Dashboard";

        RecentEvents = new PaginatedSection<RecentEvent>(
            (page, size, ct) => _dashboardService.GetRecentEventsAsync(page, size, ct));
    }

    /// <summary>
    /// Called from OnAppearing. Loads the welcome name, overview stats,
    /// insight message, and the first page of recent events in parallel.
    /// Also used by RefreshAllCommand to reload everything.
    /// </summary>
    public async Task InitializeAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            WelcomeName = FirstName(_sessionService.CurrentUser?.Name);
            await Task.WhenAll(LoadOverviewAsync(), RecentEvents.LoadAsync());
        }
        finally
        {
            IsBusy = false;
        }
    }

    // ── Commands ──────────────────────────────────────────────────────────

    [RelayCommand(CanExecute = nameof(CanRefreshAll))]
    private Task RefreshAllAsync() => InitializeAsync();

    private bool CanRefreshAll() => IsNotBusy;

    [RelayCommand]
    private void CreateEvent()
    {
        // TODO: Navigate to create-event page / show creation modal
    }

    [RelayCommand]
    private void Search()
    {
        // TODO: Navigate to search page — pass SearchQuery as the query param
    }

    protected override void OnBusyStateChanged(bool isBusy)
        => RefreshAllCommand.NotifyCanExecuteChanged();

    // ── Helpers ───────────────────────────────────────────────────────────

    private async Task LoadOverviewAsync()
    {
        var overview = await _dashboardService.GetOverviewAsync();
        if (overview is null) return;

        InsightMessage    = overview.Insight.Message;
        InsightUpdatedUtc = overview.Insight.LastUpdatedUtc;
        HasInsight        = !string.IsNullOrWhiteSpace(InsightMessage);

        TotalEvents    = overview.Stats.TotalEvents;
        TotalBookings  = overview.Stats.TotalBookings;
        TotalAttendees = overview.Stats.TotalAttendees;
        ActiveVenues   = overview.Stats.ActiveVenues;
        OccupancyRate  = string.IsNullOrWhiteSpace(overview.Stats.OccupancyRate)
                             ? "--"
                             : overview.Stats.OccupancyRate;
        SponsorsCount  = overview.Stats.Sponsors.Count;
    }

    private static string FirstName(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return "there";
        var first = fullName.Trim().Split(' ')[0];
        return string.IsNullOrEmpty(first) ? "there" : first;
    }
}
