using System.Text.Json;
using VenueOps.Models;
using VenueOps.Models.Events;

namespace VenueOps.Tests.Models;

/// <summary>
/// Pins the JSON wire names declared with [JsonPropertyName] on outgoing request models.
/// A rename on either side of the API contract should fail here, not in production.
/// Responses are covered by the service tests, which deserialize full payloads.
/// </summary>
public class ModelSerializationTests
{
    private static JsonElement Serialize<T>(T value) => JsonSerializer.SerializeToElement(value);

    private static string[] PropertyNames(JsonElement element) =>
        element.EnumerateObject().Select(p => p.Name).ToArray();

    [Fact]
    public void LoginRequest_SerializesWithCamelCaseWireNames()
    {
        JsonElement json = Serialize(new LoginRequest { Email = "kai@example.invalid", Password = new string('p', 10) });

        Assert.Equal(["email", "password"], PropertyNames(json));
        Assert.Equal("kai@example.invalid", json.GetProperty("email").GetString());
    }

    [Fact]
    public void RegisterRequest_SerializesWithCamelCaseWireNames()
    {
        JsonElement json = Serialize(new RegisterRequest { Name = "Kai Doe" });

        Assert.Equal(["name", "email", "password", "passwordConfirmation"], PropertyNames(json));
        Assert.Equal("Kai Doe", json.GetProperty("name").GetString());
    }

    [Fact]
    public void CreateEventRequest_SerializesAllFieldsWithWireNames()
    {
        CreateEventRequest request = new()
        {
            EventName = "Lantern Walk",
            VenueName = "Old Mill",
            Status = "confirmed",
            StartDateUtc = new DateTime(2030, 9, 1, 18, 0, 0, DateTimeKind.Utc),
            EndDateUtc = new DateTime(2030, 9, 1, 21, 0, 0, DateTimeKind.Utc),
            ExpectedAtendees = 120,
            ShortDescription = "A fictional evening walk.",
            Location = new EventLocation
            {
                FormattedAddress = "2 Example Lane",
                Coordinates = new EventCoordinates { Latitude = 1.25, Longitude = -2.5 }
            },
            Organizer = new EventOrganizer { Name = "Lantern Club", WebsiteUrl = "https://lanterns.example.invalid" },
            Tags = ["outdoor"],
            Links = [new EventLink { Label = "Map", Url = "https://maps.example.invalid/old-mill" }],
            Images = [new EventImage { Url = "https://images.example.invalid/lantern.jpg", AltText = "Lanterns", Credit = "Studio" }]
        };

        JsonElement json = Serialize(request);

        Assert.Equal(
            ["eventName", "venueName", "status", "startDateUtc", "endDateUtc", "expectedAtendees",
             "shortDescription", "location", "organizer", "tags", "links", "images"],
            PropertyNames(json));
        Assert.Equal(120, json.GetProperty("expectedAtendees").GetInt32());
        Assert.Equal(1.25, json.GetProperty("location").GetProperty("coordinates").GetProperty("latitude").GetDouble());
        Assert.Equal(-2.5, json.GetProperty("location").GetProperty("coordinates").GetProperty("longitude").GetDouble());
        Assert.Equal("https://lanterns.example.invalid", json.GetProperty("organizer").GetProperty("websiteUrl").GetString());
        Assert.Equal("Map", json.GetProperty("links")[0].GetProperty("label").GetString());
        Assert.Equal("Lanterns", json.GetProperty("images")[0].GetProperty("altText").GetString());
        Assert.Equal("Studio", json.GetProperty("images")[0].GetProperty("credit").GetString());
    }

    [Fact]
    public void UpdateEventRequest_SerializesAllFieldsWithWireNames()
    {
        UpdateEventRequest request = new()
        {
            Id = "evt-900",
            EventName = "Lantern Walk",
            VenueName = "Old Mill",
            Status = "completed",
            StartDateUtc = new DateTime(2030, 9, 1, 18, 0, 0, DateTimeKind.Utc),
            EndDateUtc = new DateTime(2030, 9, 1, 21, 0, 0, DateTimeKind.Utc),
            ExpectedAttendees = 120,
            AttendeeCount = 97,
            ShortDescription = "Updated fictional description.",
            Location = new EventLocation { FormattedAddress = "2 Example Lane" },
            Organizer = new EventOrganizer { Name = "Lantern Club" },
            Tags = ["outdoor"],
            Links = [],
            Images = []
        };

        JsonElement json = Serialize(request);

        // Note: the update contract spells "expectedAttendees" correctly, while the
        // create request and the detail response use "expectedAtendees".
        Assert.Equal(
            ["id", "eventName", "venueName", "status", "startDateUtc", "endDateUtc", "expectedAttendees",
             "attendeeCount", "shortDescription", "location", "organizer", "tags", "links", "images"],
            PropertyNames(json));
        Assert.Equal("evt-900", json.GetProperty("id").GetString());
        Assert.Equal(97, json.GetProperty("attendeeCount").GetInt32());
        Assert.Equal("Updated fictional description.", json.GetProperty("shortDescription").GetString());
    }

    [Fact]
    public void EventDetail_RoundTripsThroughJson()
    {
        EventDetail original = new()
        {
            Id = "evt-901",
            EventName = "Harvest Market",
            VenueName = "Barn Hall",
            Status = "completed",
            StartDateUtc = new DateTime(2030, 10, 5, 8, 0, 0, DateTimeKind.Utc),
            EndDateUtc = new DateTime(2030, 10, 5, 14, 0, 0, DateTimeKind.Utc),
            ExpectedAtendees = 400,
            AttendeeCount = 385,
            BookingReference = "BK-0901",
            ShortDescription = "Fictional produce market."
        };

        EventDetail? copy = JsonSerializer.Deserialize<EventDetail>(JsonSerializer.Serialize(original));

        Assert.NotNull(copy);
        Assert.Equal(original.Id, copy.Id);
        Assert.Equal(original.VenueName, copy.VenueName);
        Assert.Equal(original.Status, copy.Status);
        Assert.Equal(original.StartDateUtc, copy.StartDateUtc);
        Assert.Equal(original.EndDateUtc, copy.EndDateUtc);
        Assert.Equal(original.ExpectedAtendees, copy.ExpectedAtendees);
        Assert.Equal(original.AttendeeCount, copy.AttendeeCount);
        Assert.Equal(original.BookingReference, copy.BookingReference);
    }
}
