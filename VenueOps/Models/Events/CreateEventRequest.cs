using System.Text.Json.Serialization;

namespace VenueOps.Models.Events;

public sealed class CreateEventRequest
{
    [JsonPropertyName("eventName")]
    public string EventName { get; set; } = string.Empty;

    [JsonPropertyName("venueName")]
    public string VenueName { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = "draft";

    [JsonPropertyName("startDateUtc")]
    public DateTime StartDateUtc { get; set; }

    [JsonPropertyName("endDateUtc")]
    public DateTime EndDateUtc { get; set; }

    [JsonPropertyName("attendeeCount")]
    public int AttendeeCount { get; set; }
}
