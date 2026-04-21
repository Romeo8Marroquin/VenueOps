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

    [JsonPropertyName("expectedAtendees")]
    public int ExpectedAtendees { get; set; }

    [JsonPropertyName("shortDescription")]
    public string? ShortDescription { get; set; }

    [JsonPropertyName("location")]
    public EventLocation? Location { get; set; }

    [JsonPropertyName("organizer")]
    public EventOrganizer? Organizer { get; set; }

    [JsonPropertyName("tags")]
    public List<string>? Tags { get; set; }

    [JsonPropertyName("links")]
    public List<EventLink>? Links { get; set; }

    [JsonPropertyName("images")]
    public List<EventImage>? Images { get; set; }
}
