using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VenueOps.Models.Events;
using VenueOps.Services;

namespace VenueOps.ViewModels;

public partial class EventDetailViewModel : BaseViewModel, IQueryAttributable
{
    private readonly IEventsService     _eventsService;
    private readonly INavigationService _navigationService;
    private string _eventId = string.Empty;

    // ── Layout ────────────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCompactLayout))]
    public partial bool IsWideLayout { get; set; }

    public bool IsCompactLayout => !IsWideLayout;

    // ── Load state ────────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasContent))]
    public partial bool IsLoadError { get; set; }

    // ── Detail ────────────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasContent))]
    [NotifyPropertyChangedFor(nameof(HeroImageUrl))]
    [NotifyPropertyChangedFor(nameof(HasHeroImage))]
    [NotifyPropertyChangedFor(nameof(HeroImageCredit))]
    [NotifyPropertyChangedFor(nameof(HasImageCredit))]
    [NotifyPropertyChangedFor(nameof(HasDescription))]
    [NotifyPropertyChangedFor(nameof(HasLocation))]
    [NotifyPropertyChangedFor(nameof(HasOrganizer))]
    [NotifyPropertyChangedFor(nameof(OrganizerHasWebsite))]
    [NotifyPropertyChangedFor(nameof(HasTags))]
    [NotifyPropertyChangedFor(nameof(HasLinks))]
    [NotifyPropertyChangedFor(nameof(DateRangeDisplay))]
    [NotifyPropertyChangedFor(nameof(AttendeesLabel))]
    [NotifyPropertyChangedFor(nameof(AttendeesDisplay))]
    [NotifyPropertyChangedFor(nameof(GalleryImages))]
    [NotifyPropertyChangedFor(nameof(HasGalleryImages))]
    public partial EventDetail? Detail { get; set; }

    // ── Computed ──────────────────────────────────────────────────────────

    public bool HasContent      => !IsBusy && !IsLoadError && Detail is not null;
    public string? HeroImageUrl => Detail?.Images.FirstOrDefault()?.Url;
    public bool HasHeroImage    => !string.IsNullOrEmpty(HeroImageUrl);
    public string? HeroImageCredit => Detail?.Images.FirstOrDefault()?.Credit;
    public bool HasImageCredit  => !string.IsNullOrEmpty(HeroImageCredit);
    public bool HasDescription  => !string.IsNullOrWhiteSpace(Detail?.ShortDescription);
    public bool HasLocation     => Detail?.Location is { } loc && !string.IsNullOrWhiteSpace(loc.FormattedAddress);
    public bool HasOrganizer    => Detail?.Organizer is { } org && !string.IsNullOrWhiteSpace(org.Name);
    public bool OrganizerHasWebsite => !string.IsNullOrWhiteSpace(Detail?.Organizer?.WebsiteUrl);
    public bool HasTags         => Detail?.Tags is { Count: > 0 };
    public bool HasLinks        => Detail?.Links is { Count: > 0 };
    public IReadOnlyList<EventImage> GalleryImages =>
        Detail?.Images is { Count: > 1 }
            ? (IReadOnlyList<EventImage>)Detail.Images.Skip(1).ToList()
            : Array.Empty<EventImage>();
    public bool HasGalleryImages => GalleryImages.Count > 0;

    public string DateRangeDisplay
    {
        get
        {
            if (Detail is null) return string.Empty;
            var start = Detail.StartDateUtc.ToLocalTime();
            var end   = Detail.EndDateUtc.ToLocalTime();
            if (start.Date == end.Date)
                return $"{start:MMM d, yyyy}  ·  {start:h:mm tt} – {end:h:mm tt}";
            return $"{start:MMM d} – {end:MMM d, yyyy}";
        }
    }

    private static readonly HashSet<string> _finalStatuses =
        new(StringComparer.OrdinalIgnoreCase) { "completed", "cancelled" };

    public string AttendeesLabel =>
        Detail is not null && _finalStatuses.Contains(Detail.Status)
            ? "Attendees"
            : "Exp. Attendees";

    public string AttendeesDisplay =>
        Detail is null
            ? string.Empty
            : _finalStatuses.Contains(Detail.Status)
                ? $"{Detail.AttendeeCount:N0}"
                : $"{Detail.ExpectedAtendees:N0}";

    // ── Constructor ───────────────────────────────────────────────────────

    public EventDetailViewModel(IEventsService eventsService, INavigationService navigationService)
    {
        _eventsService     = eventsService;
        _navigationService = navigationService;
        Title = "Event Detail";
    }

    // ── IQueryAttributable ────────────────────────────────────────────────

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("id", out var val) && val is string id)
            _eventId = Uri.UnescapeDataString(id);
    }

    // ── Load ──────────────────────────────────────────────────────────────

    public async Task InitializeAsync()
    {
        if (string.IsNullOrEmpty(_eventId) || IsBusy) return;

        IsBusy      = true;
        IsLoadError = false;
        Detail      = null;

        try
        {
            Detail      = await _eventsService.GetEventDetailAsync(_eventId);
            IsLoadError = Detail is null;
        }
        catch
        {
            IsLoadError = true;
        }
        finally
        {
            IsBusy = false;
        }
    }

    // ── Commands ──────────────────────────────────────────────────────────

    [RelayCommand]
    private Task GoBackAsync() => _navigationService.GoBackAsync();

    [RelayCommand]
    private Task RetryAsync() => InitializeAsync();

    [RelayCommand]
    private async Task EditEventAsync()
    {
        if (Detail is null) return;
        var snapshot = Detail;
        await _navigationService.PushEditEventModalAsync(snapshot, InitializeAsync);
    }

    [RelayCommand]
    private static async Task OpenLinkAsync(string? url)
    {
        if (!string.IsNullOrWhiteSpace(url) && Uri.TryCreate(url, UriKind.Absolute, out var uri))
            await Launcher.Default.OpenAsync(uri);
    }

    // ── Hooks ─────────────────────────────────────────────────────────────

    protected override void OnBusyStateChanged(bool isBusy)
        => OnPropertyChanged(nameof(HasContent));
}
