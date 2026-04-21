using System.Collections.ObjectModel;
using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VenueOps.Models.Events;
using VenueOps.Services;

namespace VenueOps.ViewModels;

public partial class CreateEventViewModel : BaseViewModel
{
    private readonly IEventsService _eventsService;
    private readonly IPopupService _popupService;

    // ── Layout ────────────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCompactLayout))]
    public partial bool IsWideLayout { get; set; }

    public bool IsCompactLayout => !IsWideLayout;

    // ── Core fields ───────────────────────────────────────────────────────

    [ObservableProperty]
    public partial string EventName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string VenueName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Status { get; set; } = "Draft";

    [ObservableProperty]
    public partial string ExpectedAttendeesText { get; set; } = string.Empty;

    // ── Dates ─────────────────────────────────────────────────────────────

    [ObservableProperty]
    public partial DateTime StartDate { get; set; } = DateTime.Today;

    [ObservableProperty]
    public partial TimeSpan StartTime { get; set; } = new TimeSpan(9, 0, 0);

    [ObservableProperty]
    public partial DateTime EndDate { get; set; } = DateTime.Today;

    [ObservableProperty]
    public partial TimeSpan EndTime { get; set; } = new TimeSpan(10, 0, 0);

    // ── Optional fields ───────────────────────────────────────────────────

    [ObservableProperty]
    public partial string ShortDescription { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string LocationAddress { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string OrganizerName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string OrganizerWebsite { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string TagsText { get; set; } = string.Empty;

    // ── Dynamic collections ───────────────────────────────────────────────

    public ObservableCollection<LinkItem> Links { get; } = [];
    public ObservableCollection<ImageItem> Images { get; } = [];

    // ── Validation feedback ───────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string ErrorMessage { get; set; } = string.Empty;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    // ── Options ───────────────────────────────────────────────────────────

    public IReadOnlyList<string> StatusOptions { get; } = ["Draft", "Confirmed", "Cancelled"];

    public CreateEventViewModel(IEventsService eventsService, IPopupService popupService)
    {
        _eventsService = eventsService;
        _popupService = popupService;
        Title = "New Event";
    }

    // ── Commands — collections ────────────────────────────────────────────

    [RelayCommand]
    private void AddLink() => Links.Add(new LinkItem());

    [RelayCommand]
    private void RemoveLink(LinkItem item) => Links.Remove(item);

    [RelayCommand]
    private void AddImage() => Images.Add(new ImageItem());

    [RelayCommand]
    private void RemoveImage(ImageItem item) => Images.Remove(item);

    // ── Commands — submit / cancel ────────────────────────────────────────

    [RelayCommand(CanExecute = nameof(CanSubmit))]
    private async Task SubmitAsync()
    {
        if (!Validate()) return;

        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            var startLocal = StartDate.Date + StartTime;
            var endLocal   = EndDate.Date   + EndTime;

            var tags = TagsText
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(t => !string.IsNullOrEmpty(t))
                .ToList();

            var links = Links
                .Where(l => !string.IsNullOrWhiteSpace(l.Label) && !string.IsNullOrWhiteSpace(l.Url))
                .Select(l => new EventLink { Label = l.Label.Trim(), Url = l.Url.Trim() })
                .ToList();

            var images = Images
                .Where(i => !string.IsNullOrWhiteSpace(i.Url))
                .Select(i => new EventImage { Url = i.Url.Trim(), AltText = i.AltText.Trim(), Credit = i.Credit.Trim() })
                .ToList();

            var request = new CreateEventRequest
            {
                EventName        = EventName.Trim(),
                VenueName        = VenueName.Trim(),
                Status           = Status.ToLowerInvariant(),
                StartDateUtc     = startLocal.ToUniversalTime(),
                EndDateUtc       = endLocal.ToUniversalTime(),
                ExpectedAtendees = int.TryParse(ExpectedAttendeesText, out var count) ? count : 0,
                ShortDescription = string.IsNullOrWhiteSpace(ShortDescription) ? null : ShortDescription.Trim(),
                Location         = string.IsNullOrWhiteSpace(LocationAddress)
                                       ? null
                                       : new EventLocation { FormattedAddress = LocationAddress.Trim() },
                Organizer        = string.IsNullOrWhiteSpace(OrganizerName)
                                       ? null
                                       : new EventOrganizer
                                         {
                                             Name       = OrganizerName.Trim(),
                                             WebsiteUrl = string.IsNullOrWhiteSpace(OrganizerWebsite) ? null : OrganizerWebsite.Trim()
                                         },
                Tags   = tags.Count   > 0 ? tags   : null,
                Links  = links.Count  > 0 ? links  : null,
                Images = images.Count > 0 ? images : null,
            };

            var result = await _eventsService.CreateEventAsync(request);

            IsBusy = false;

            if (result is null)
            {
                ErrorMessage = "Something went wrong. Please try again.";
                return;
            }

            var hostPage = Application.Current?.Windows[0].Page;
            if (hostPage is not null)
            {
                await hostPage.DisplayAlertAsync(
                    "Event Created",
                    $"\"{result.EventName}\" was created successfully.\nRef: {result.BookingReference}",
                    "OK");
            }

            await _popupService.ClosePopupAsync(Shell.Current, true);
        }
        catch (Exception)
        {
            IsBusy = false;
            ErrorMessage = "Something went wrong. Please try again.";
        }
    }

    private bool CanSubmit() => IsNotBusy;

    [RelayCommand]
    private async Task CancelAsync()
    {
        await _popupService.ClosePopupAsync(Shell.Current, false);
    }

    protected override void OnBusyStateChanged(bool isBusy)
        => SubmitCommand.NotifyCanExecuteChanged();

    // ── Validation ────────────────────────────────────────────────────────

    private bool Validate()
    {
        if (string.IsNullOrWhiteSpace(EventName))
        {
            ErrorMessage = "Event name is required.";
            return false;
        }
        if (string.IsNullOrWhiteSpace(VenueName))
        {
            ErrorMessage = "Venue name is required.";
            return false;
        }

        var startLocal = StartDate.Date + StartTime;
        var endLocal   = EndDate.Date   + EndTime;
        if (endLocal <= startLocal)
        {
            ErrorMessage = "End date/time must be after start date/time.";
            return false;
        }

        if (!string.IsNullOrEmpty(ExpectedAttendeesText)
            && (!int.TryParse(ExpectedAttendeesText, out var count) || count < 0))
        {
            ErrorMessage = "Expected attendees must be a non-negative number.";
            return false;
        }

        return true;
    }
}

// ── Collection item types ─────────────────────────────────────────────────────

public partial class LinkItem : ObservableObject
{
    [ObservableProperty]
    public partial string Label { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Url { get; set; } = string.Empty;
}

public partial class ImageItem : ObservableObject
{
    [ObservableProperty]
    public partial string Url { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string AltText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Credit { get; set; } = string.Empty;
}
