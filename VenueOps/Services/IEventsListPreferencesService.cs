namespace VenueOps.Services;

/// <summary>
/// Remembers the organiser's choices for the events list while the app is running,
/// so a new events list view model starts with the choice made earlier.
/// </summary>
public interface IEventsListPreferencesService
{
    /// <summary>
    /// True when the events list hides events that have already ended.
    /// Off by default. Kept in memory only.
    /// </summary>
    bool UpcomingOnly { get; set; }
}
