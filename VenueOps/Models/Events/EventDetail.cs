using System.Text.Json.Serialization;

namespace VenueOps.Models.Events;

public sealed class EventDetail
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

    [JsonPropertyName("expectedAtendees")]
    public int ExpectedAtendees { get; set; }

    [JsonPropertyName("attendeeCount")]
    public int AttendeeCount { get; set; }

    [JsonPropertyName("bookingReference")]
    public string BookingReference { get; set; } = string.Empty;

    [JsonPropertyName("shortDescription")]
    public string ShortDescription { get; set; } = string.Empty;

    [JsonPropertyName("images")]
    public List<EventImage> Images { get; set; } = [];

    [JsonPropertyName("location")]
    public EventLocation? Location { get; set; }

    [JsonPropertyName("organizer")]
    public EventOrganizer? Organizer { get; set; }

    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = [];

    [JsonPropertyName("links")]
    public List<EventLink> Links { get; set; } = [];
}

public sealed class EventImage
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("altText")]
    public string AltText { get; set; } = string.Empty;

    [JsonPropertyName("credit")]
    public string Credit { get; set; } = string.Empty;
}

public sealed class EventLocation
{
    [JsonPropertyName("formattedAddress")]
    public string FormattedAddress { get; set; } = string.Empty;

    [JsonPropertyName("coordinates")]
    public EventCoordinates? Coordinates { get; set; }
}

public sealed class EventCoordinates
{
    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }
}

public sealed class EventOrganizer
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("websiteUrl")]
    public string? WebsiteUrl { get; set; }
}

public sealed class EventLink
{
    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
}
