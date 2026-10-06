using System.Globalization;
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
    private readonly IClipboardService  _clipboard;

    // First row of the exported CSV. Column order matches the values written by BuildCsv.
    private const string CsvHeader = "Event,Venue,Status,Start (UTC),End (UTC),Attendees,Booking reference";

    // CSV lines are separated by CRLF (RFC 4180) so spreadsheets read them the same way everywhere.
    private const string CsvLineBreak = "\r\n";

    private const string CsvUtcFormat = "yyyy-MM-dd'T'HH:mm:ss'Z'";

    private static readonly char[] CsvSpecialCharacters = [',', '"', '\r', '\n'];

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
        IEventsListPreferencesService preferences,
        IClipboardService clipboard)
    {
        _eventsService     = eventsService;
        _navigationService = navigationService;
        _preferences       = preferences;
        _clipboard         = clipboard;
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

        // The button is enabled only while the list holds events, so re-evaluate when it changes.
        Events.Items.CollectionChanged += (_, _) => CopyAsCsvCommand.NotifyCanExecuteChanged();
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

    // Copies the events currently listed (already searched, filtered, sorted and paged by the
    // backend) to the clipboard as CSV. It does not call the events service again.
    [RelayCommand(CanExecute = nameof(CanCopyAsCsv))]
    private Task CopyAsCsvAsync() => _clipboard.SetTextAsync(BuildCsv(Events.Items));

    private bool CanCopyAsCsv() => Events.Items.Count > 0;

    partial void OnEventsStatusFilterChanged(string value) => _ = Events.LoadAsync();
    partial void OnSortByFilterChanged(string value)       => _ = Events.LoadAsync();

    // ── CSV export ────────────────────────────────────────────────────────

    private static string BuildCsv(IEnumerable<RecentEvent> events)
    {
        List<string> lines = [CsvHeader];

        foreach (RecentEvent evt in events)
        {
            lines.Add(string.Join(',',
                EscapeCsvField(evt.EventName),
                EscapeCsvField(evt.VenueName),
                EscapeCsvField(evt.Status),
                EscapeCsvField(FormatUtc(evt.StartDateUtc)),
                EscapeCsvField(FormatUtc(evt.EndDateUtc)),
                EscapeCsvField(evt.AttendeeCount.ToString(CultureInfo.InvariantCulture)),
                EscapeCsvField(evt.BookingReference)));
        }

        return string.Join(CsvLineBreak, lines);
    }

    // The API sends UTC. A Local value is converted; Utc and Unspecified are taken as UTC already.
    private static string FormatUtc(DateTime value)
    {
        DateTime utc = value.Kind == DateTimeKind.Local ? value.ToUniversalTime() : value;
        return utc.ToString(CsvUtcFormat, CultureInfo.InvariantCulture);
    }

    private static string EscapeCsvField(string? field)
    {
        if (string.IsNullOrEmpty(field)) return string.Empty;

        return field.IndexOfAny(CsvSpecialCharacters) >= 0
            ? $"\"{field.Replace("\"", "\"\"")}\""
            : field;
    }
}
