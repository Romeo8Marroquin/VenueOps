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
    // Set by CreateEventView based on popup width after sizing.

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCompactLayout))]
    public partial bool IsWideLayout { get; set; }

    public bool IsCompactLayout => !IsWideLayout;

    // ── Form fields ───────────────────────────────────────────────────────

    [ObservableProperty]
    public partial string EventName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string VenueName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Status { get; set; } = "draft";

    [ObservableProperty]
    public partial DateTime StartDate { get; set; } = DateTime.Today;

    [ObservableProperty]
    public partial TimeSpan StartTime { get; set; } = new TimeSpan(9, 0, 0);

    [ObservableProperty]
    public partial DateTime EndDate { get; set; } = DateTime.Today;

    [ObservableProperty]
    public partial TimeSpan EndTime { get; set; } = new TimeSpan(10, 0, 0);

    [ObservableProperty]
    public partial string AttendeeCountText { get; set; } = "0";

    // ── Validation feedback ───────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string ErrorMessage { get; set; } = string.Empty;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    // ── Options ───────────────────────────────────────────────────────────

    public IReadOnlyList<string> StatusOptions { get; } = ["draft", "confirmed", "cancelled"];

    public CreateEventViewModel(IEventsService eventsService, IPopupService popupService)
    {
        _eventsService = eventsService;
        _popupService = popupService;
        Title = "New Event";
    }

    // ── Commands ──────────────────────────────────────────────────────────

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

            var request = new CreateEventRequest
            {
                EventName     = EventName.Trim(),
                VenueName     = VenueName.Trim(),
                Status        = Status,
                StartDateUtc  = startLocal.ToUniversalTime(),
                EndDateUtc    = endLocal.ToUniversalTime(),
                AttendeeCount = int.TryParse(AttendeeCountText, out var count) ? count : 0
            };

            var result = await _eventsService.CreateEventAsync(request);
            if (result is null)
            {
                ErrorMessage = "Something went wrong. Please try again.";
                return;
            }

            // Show success alert. Calling DisplayAlertAsync on the host window page
            // renders a native dialog above the popup on all platforms.
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
            ErrorMessage = "Something went wrong. Please try again.";
        }
        finally
        {
            IsBusy = false;
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
        if (!int.TryParse(AttendeeCountText, out var count) || count < 0)
        {
            ErrorMessage = "Expected attendees must be a non-negative number.";
            return false;
        }

        var startLocal = StartDate.Date + StartTime;
        var endLocal   = EndDate.Date   + EndTime;
        if (endLocal <= startLocal)
        {
            ErrorMessage = "End date/time must be after start date/time.";
            return false;
        }

        return true;
    }
}
