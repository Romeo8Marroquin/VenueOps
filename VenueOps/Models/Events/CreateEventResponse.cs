using System.Text.Json.Serialization;

namespace VenueOps.Models.Events;

public sealed class CreateEventResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("eventName")]
    public string EventName { get; set; } = string.Empty;

    [JsonPropertyName("venueName")]
    public string VenueName { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("startDateUtc")]
    public DateTime StartDateUtc { get; set; }

    [JsonPropertyName("endDateUtc")]
    public DateTime EndDateUtc { get; set; }

    [JsonPropertyName("attendeeCount")]
    public int AttendeeCount { get; set; }

    [JsonPropertyName("bookingReference")]
    public string BookingReference { get; set; } = string.Empty;

    [JsonPropertyName("createdAtUtc")]
    public DateTime CreatedAtUtc { get; set; }

    [JsonPropertyName("updatedAtUtc")]
    public DateTime UpdatedAtUtc { get; set; }
}
