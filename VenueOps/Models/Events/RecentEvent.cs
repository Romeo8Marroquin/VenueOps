using System.Text.Json.Serialization;

namespace VenueOps.Models.Events;

public sealed class RecentEvent
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("eventName")]
    public string EventName { get; set; } = string.Empty;

    [JsonPropertyName("venueName")]
    public string VenueName { get; set; } = string.Empty;

    /// <summary>confirmed | draft | completed | cancelled</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>UTC — convert to device local time before display.</summary>
    [JsonPropertyName("startDateUtc")]
    public DateTime StartDateUtc { get; set; }

    /// <summary>UTC — convert to device local time before display.</summary>
    [JsonPropertyName("endDateUtc")]
    public DateTime EndDateUtc { get; set; }

    [JsonPropertyName("attendeeCount")]
    public int AttendeeCount { get; set; }

    [JsonPropertyName("bookingReference")]
    public string BookingReference { get; set; } = string.Empty;
}
