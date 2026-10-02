namespace VenueOps.Services;

/// <summary>
/// In-memory implementation. Registered as a singleton, so the value lives as long as the app
/// process. It is not written to device storage and is not per user.
/// </summary>
public sealed class EventsListPreferencesService : IEventsListPreferencesService
{
    public bool UpcomingOnly { get; set; }
}
