using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using VenueOps.Models.Events;
using VenueOps.Services;
using VenueOps.ViewModels.Common;

namespace VenueOps.ViewModels;

public partial class EventsViewModel : BaseViewModel, IQueryAttributable
{
    private readonly IEventsService     _eventsService;
    private readonly INavigationService _navigationService;
    private readonly IEventsListPreferencesService _preferences;

    // Maps the label shown in the UI to the backend field name expected by the API
    private static readonly Dictionary<string, string> SortFieldMap = new()
    {
        { "Date",        "startDateUtc"    },
        { "Event",       "eventName"       },
        { "Venue",       "venueName"       },
        { "Attendees",   "attendeeCount"   },
        { "Booking Ref", "bookingReference" },
        { "Status",      "status"          },
    };

    // ── Layout ────────────────────────────────────────────────────────────
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCompactLayout))]
    public partial bool IsWideLayout { get; set; }

    public bool IsCompactLayout => !IsWideLayout;

    // ── Search + filter ───────────────────────────────────────────────────
    [ObservableProperty]
    public partial string EventsQuery { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string EventsStatusFilter { get; set; } = "All";

    public IReadOnlyList<string> StatusFilters { get; } =
        ["All", "Confirmed", "Draft", "Completed", "Cancelled"];

    // ── Sort ──────────────────────────────────────────────────────────────
    [ObservableProperty]
    public partial string SortByFilter { get; set; } = "Date";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SortDirectionSymbol))]
    public partial bool SortAscending { get; set; } // false = desc (default matches API default)

    public string SortDirectionSymbol => SortAscending ? "↑" : "↓";

    public IReadOnlyList<string> SortByFields { get; } =
        ["Date", "Event", "Venue", "Attendees", "Booking Ref", "Status"];

    // ── Upcoming only ─────────────────────────────────────────────────────
    // Hides events whose end date has passed (the backend decides, not the client). The choice is
    // kept in the shared preferences service, so a new view model starts with the same value.
    // Not an [ObservableProperty]: the initial value is read from the service in the constructor
    // and must not trigger a load.
    private bool _upcomingOnly;

    public bool UpcomingOnly
    {
        get => _upcomingOnly;
        set
        {
            if (!SetProperty(ref _upcomingOnly, value)) return;
            _preferences.UpcomingOnly = value;
            _ = Events.LoadAsync();
        }
    }

    // ── Paginated events ──────────────────────────────────────────────────
    public PaginatedSection<RecentEvent> Events { get; }

    public EventsViewModel(
        IEventsService eventsService,
        INavigationService navigationService,
        IEventsListPreferencesService preferences)
    {
        _eventsService     = eventsService;
        _navigationService = navigationService;
        _preferences       = preferences;
        _upcomingOnly      = preferences.UpcomingOnly;
        Title = "Event List";

        Events = new PaginatedSection<RecentEvent>(
            (page, size, ct) => _eventsService.GetEventsAsync(
                page, size,
                string.IsNullOrWhiteSpace(EventsQuery) ? null : EventsQuery,
                EventsStatusFilter == "All" ? null : EventsStatusFilter.ToLowerInvariant(),
                SortFieldMap.TryGetValue(SortByFilter, out var sf) ? sf : "startDateUtc",
                SortAscending ? "asc" : "desc",
                UpcomingOnly,
                ct),
            pageSize: 10);
    }

    // ApplyQueryAttributes is called by Shell before OnAppearing, so the query
    // is already set when InitializeAsync runs from OnAppearing.
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("query", out var val) && val is string str && !string.IsNullOrEmpty(str))
            EventsQuery = Uri.UnescapeDataString(str);
    }

    public async Task InitializeAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try { await Events.LoadAsync(); }
        finally { IsBusy = false; }
    }

    // ── Commands ──────────────────────────────────────────────────────────

    [RelayCommand]
    private Task SearchEventsAsync() => Events.LoadAsync();

    [RelayCommand]
    private Task ToggleSortDirectionAsync()
    {
        SortAscending = !SortAscending;
        return Events.LoadAsync();
    }

    [RelayCommand]
    private Task CreateEventAsync()
        => _navigationService.PushCreateEventModalAsync(
            () => Events.LoadAsync());

    [RelayCommand]
    private Task GoToEventDetailAsync(RecentEvent evt)
        => _navigationService.GoToEventDetailAsync(evt.Id);

    partial void OnEventsStatusFilterChanged(string value) => _ = Events.LoadAsync();
    partial void OnSortByFilterChanged(string value)       => _ = Events.LoadAsync();
}
