using System.Net;
using System.Text.Json;
using Moq;
using VenueOps.Models.Common;
using VenueOps.Models.Events;
using VenueOps.Services;
using VenueOps.Tests.TestDoubles;

namespace VenueOps.Tests.Services;

public class EventsServiceTests
{
    private const string EventListJson =
        """
        {
          "items": [
            { "id": "evt-010", "eventName": "Maple Street Fair", "venueName": "Town Square",
              "status": "draft", "startDateUtc": "2030-06-10T09:00:00Z", "endDateUtc": "2030-06-10T17:00:00Z",
              "attendeeCount": 0, "bookingReference": "BK-0010" },
            { "id": "evt-011", "eventName": "Quiet Pines Retreat", "venueName": "Lakeside Lodge",
              "status": "confirmed", "startDateUtc": "2030-06-12T09:00:00Z", "endDateUtc": "2030-06-14T17:00:00Z",
              "attendeeCount": 40, "bookingReference": "BK-0011" }
          ],
          "total": 2, "page": 1, "pageSize": 10, "hasMore": false
        }
        """;

    private const string CreatedJson =
        """
        {
          "id": "evt-020", "eventName": "Copper Kettle Expo", "venueName": "North Hall", "status": "draft",
          "startDateUtc": "2030-07-01T10:00:00Z", "endDateUtc": "2030-07-01T16:00:00Z",
          "attendeeCount": 0, "bookingReference": "BK-0020",
          "createdAtUtc": "2030-01-15T12:00:00Z", "updatedAtUtc": "2030-01-15T12:05:00Z"
        }
        """;

    private const string DetailJson =
        """
        {
          "id": "evt/030", "eventName": "Silver Birch Concert", "venueName": "Glass Pavilion", "status": "confirmed",
          "startDateUtc": "2030-08-20T19:00:00Z", "endDateUtc": "2030-08-20T22:00:00Z",
          "expectedAtendees": 300, "attendeeCount": 0, "bookingReference": "BK-0030",
          "shortDescription": "An evening of fictional music.",
          "images": [ { "url": "https://images.example.invalid/hero.jpg", "altText": "Stage", "credit": "Photo Studio" } ],
          "location": { "formattedAddress": "1 Example Way", "coordinates": { "latitude": 10.5, "longitude": -20.25 } },
          "organizer": { "name": "Fictional Events Co", "websiteUrl": "https://organizer.example.invalid" },
          "tags": ["music", "evening"],
          "links": [ { "label": "Tickets", "url": "https://tickets.example.invalid/evt-030" } ]
        }
        """;

    private static EventsService CreateSut(StubHttpMessageHandler handler) => new(FakeApi.CreateFactory(handler).Object);

    [Fact]
    public void Constructor_CreatesNamedApiClient()
    {
        Mock<IHttpClientFactory> factory = FakeApi.CreateFactory(FakeApi.Respond(HttpStatusCode.OK));

        _ = new EventsService(factory.Object);

        factory.Verify(f => f.CreateClient(FakeApi.ClientName), Times.Once);
    }

    // ── GetEventsAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetEventsAsync_UsesDefaults_WhenOnlyPagingIsProvided()
    {
        StubHttpMessageHandler handler = FakeApi.Respond(HttpStatusCode.OK, EventListJson);

        await CreateSut(handler).GetEventsAsync(1, 10);

        Assert.Equal(HttpMethod.Get, handler.LastRequest.Method);
        Assert.Equal(
            "events/list?query=&status=all&venueUuid=&page=1&pageSize=10&sortBy=startDateUtc&sortDirection=desc",
            handler.LastRequest.RelativeUrl);
    }

    [Fact]
    public async Task GetEventsAsync_EscapesQueryAndPassesStatusAndSort_WhenProvided()
    {
        StubHttpMessageHandler handler = FakeApi.Respond(HttpStatusCode.OK, EventListJson);

        await CreateSut(handler).GetEventsAsync(2, 10, "fair/expo", "draft", "eventName", "asc");

        Assert.Equal(
            "events/list?query=fair%2Fexpo&status=draft&venueUuid=&page=2&pageSize=10&sortBy=eventName&sortDirection=asc",
            handler.LastRequest.RelativeUrl);
    }

    [Fact]
    public async Task GetEventsAsync_FallsBackToAllStatus_WhenStatusIsWhitespace()
    {
        StubHttpMessageHandler handler = FakeApi.Respond(HttpStatusCode.OK, EventListJson);

        await CreateSut(handler).GetEventsAsync(1, 10, status: " ");

        Assert.Contains("status=all&", handler.LastRequest.RelativeUrl);
    }

    [Fact]
    public async Task GetEventsAsync_DeserializesPagedResult_OnSuccess()
    {
        PagedResult<RecentEvent>? result = await CreateSut(FakeApi.Respond(HttpStatusCode.OK, EventListJson))
            .GetEventsAsync(1, 10);

        Assert.NotNull(result);
        Assert.Equal(2, result.Total);
        Assert.False(result.HasMore);
        Assert.Equal(["evt-010", "evt-011"], result.Items.Select(e => e.Id));
    }

    [Fact]
    public async Task GetEventsAsync_ReturnsNull_OnNonSuccessStatus()
    {
        Assert.Null(await CreateSut(FakeApi.Respond(HttpStatusCode.InternalServerError)).GetEventsAsync(1, 10));
    }

    // ── GetEventsAsync: upcomingOnly ──────────────────────────────────────────

    [Fact]
    public async Task GetEventsAsync_SendsUpcomingOnlyTrue_WhenRequested()
    {
        StubHttpMessageHandler handler = FakeApi.Respond(HttpStatusCode.OK, EventListJson);

        await CreateSut(handler).GetEventsAsync(1, 10, upcomingOnly: true);

        Assert.Equal(HttpMethod.Get, handler.LastRequest.Method);
        Assert.StartsWith("events/list?", handler.LastRequest.RelativeUrl);
        Assert.Contains("upcomingOnly=true", handler.LastRequest.Uri.Query.TrimStart('?').Split('&'));
        Assert.Equal(
            "events/list?query=&status=all&venueUuid=&page=1&pageSize=10&sortBy=startDateUtc&sortDirection=desc&upcomingOnly=true",
            handler.LastRequest.RelativeUrl);
    }

    [Fact]
    public async Task GetEventsAsync_OmitsUpcomingOnly_WhenParameterIsNotProvided()
    {
        StubHttpMessageHandler handler = FakeApi.Respond(HttpStatusCode.OK, EventListJson);

        await CreateSut(handler).GetEventsAsync(1, 10);

        Assert.DoesNotContain("upcomingOnly", handler.LastRequest.RelativeUrl);
    }

    [Fact]
    public async Task GetEventsAsync_OmitsUpcomingOnly_WhenFalse()
    {
        StubHttpMessageHandler handler = FakeApi.Respond(HttpStatusCode.OK, EventListJson);

        await CreateSut(handler).GetEventsAsync(1, 10, upcomingOnly: false);

        Assert.DoesNotContain("upcomingOnly", handler.LastRequest.RelativeUrl);
        Assert.Equal(
            "events/list?query=&status=all&venueUuid=&page=1&pageSize=10&sortBy=startDateUtc&sortDirection=desc",
            handler.LastRequest.RelativeUrl);
    }

    [Fact]
    public async Task GetEventsAsync_WithUpcomingOnly_DeserializesPagedResult_OnSuccess()
    {
        PagedResult<RecentEvent>? result = await CreateSut(FakeApi.Respond(HttpStatusCode.OK, EventListJson))
            .GetEventsAsync(1, 10, upcomingOnly: true);

        Assert.NotNull(result);
        Assert.Equal(2, result.Total);
        Assert.False(result.HasMore);
        Assert.Equal(["evt-010", "evt-011"], result.Items.Select(e => e.Id));
    }

    [Fact]
    public async Task GetEventsAsync_WithUpcomingOnly_ReturnsNull_OnNonSuccessStatus()
    {
        Assert.Null(await CreateSut(FakeApi.Respond(HttpStatusCode.InternalServerError))
            .GetEventsAsync(1, 10, upcomingOnly: true));
    }

    [Fact]
    public async Task GetEventsAsync_SendsEveryFilterTogether_WhenUpcomingOnlyIsCombined()
    {
        StubHttpMessageHandler handler = FakeApi.Respond(HttpStatusCode.OK, EventListJson);

        await CreateSut(handler).GetEventsAsync(3, 25, "fair/expo", "confirmed", "venueName", "asc", upcomingOnly: true);

        Assert.Single(handler.Requests);
        Assert.Equal(
            "events/list?query=fair%2Fexpo&status=confirmed&venueUuid=&page=3&pageSize=25&sortBy=venueName&sortDirection=asc&upcomingOnly=true",
            handler.LastRequest.RelativeUrl);
    }

    // ── CreateEventAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task CreateEventAsync_PostsRequestToNewEndpoint()
    {
        StubHttpMessageHandler handler = FakeApi.Respond(HttpStatusCode.OK, CreatedJson);
        CreateEventRequest request = new()
        {
            EventName = "Copper Kettle Expo",
            VenueName = "North Hall",
            StartDateUtc = new DateTime(2030, 7, 1, 10, 0, 0, DateTimeKind.Utc),
            EndDateUtc = new DateTime(2030, 7, 1, 16, 0, 0, DateTimeKind.Utc),
            ExpectedAtendees = 80
        };

        await CreateSut(handler).CreateEventAsync(request);

        Assert.Equal(HttpMethod.Post, handler.LastRequest.Method);
        Assert.Equal("events/new", handler.LastRequest.RelativeUrl);
        using JsonDocument body = JsonDocument.Parse(handler.LastRequest.Body!);
        Assert.Equal("Copper Kettle Expo", body.RootElement.GetProperty("eventName").GetString());
        Assert.Equal("draft", body.RootElement.GetProperty("status").GetString());
        Assert.Equal(80, body.RootElement.GetProperty("expectedAtendees").GetInt32());
    }

    [Fact]
    public async Task CreateEventAsync_ReturnsDeserializedResponse_OnSuccess()
    {
        CreateEventResponse? result = await CreateSut(FakeApi.Respond(HttpStatusCode.Created, CreatedJson))
            .CreateEventAsync(new CreateEventRequest());

        Assert.NotNull(result);
        Assert.Equal("evt-020", result.Id);
        Assert.Equal("Copper Kettle Expo", result.EventName);
        Assert.Equal("North Hall", result.VenueName);
        Assert.Equal("draft", result.Status);
        Assert.Equal(new DateTime(2030, 7, 1, 10, 0, 0, DateTimeKind.Utc), result.StartDateUtc);
        Assert.Equal(new DateTime(2030, 7, 1, 16, 0, 0, DateTimeKind.Utc), result.EndDateUtc);
        Assert.Equal(0, result.AttendeeCount);
        Assert.Equal("BK-0020", result.BookingReference);
        Assert.Equal(new DateTime(2030, 1, 15, 12, 0, 0, DateTimeKind.Utc), result.CreatedAtUtc);
        Assert.Equal(new DateTime(2030, 1, 15, 12, 5, 0, DateTimeKind.Utc), result.UpdatedAtUtc);
    }

    [Fact]
    public async Task CreateEventAsync_ReturnsNull_OnNonSuccessStatus()
    {
        Assert.Null(await CreateSut(FakeApi.Respond(HttpStatusCode.BadRequest)).CreateEventAsync(new CreateEventRequest()));
    }

    // ── GetEventDetailAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetEventDetailAsync_EscapesIdInQueryString()
    {
        StubHttpMessageHandler handler = FakeApi.Respond(HttpStatusCode.OK, DetailJson);

        await CreateSut(handler).GetEventDetailAsync("evt/030 a&b");

        Assert.Equal(HttpMethod.Get, handler.LastRequest.Method);
        Assert.Equal("events/detail?id=evt%2F030%20a%26b", handler.LastRequest.RelativeUrl);
    }

    [Fact]
    public async Task GetEventDetailAsync_DeserializesFullDetail_OnSuccess()
    {
        EventDetail? result = await CreateSut(FakeApi.Respond(HttpStatusCode.OK, DetailJson))
            .GetEventDetailAsync("evt/030");

        Assert.NotNull(result);
        Assert.Equal("evt/030", result.Id);
        Assert.Equal("Silver Birch Concert", result.EventName);
        Assert.Equal(300, result.ExpectedAtendees);
        Assert.Equal("An evening of fictional music.", result.ShortDescription);
        EventImage image = Assert.Single(result.Images);
        Assert.Equal("https://images.example.invalid/hero.jpg", image.Url);
        Assert.NotNull(result.Location?.Coordinates);
        Assert.Equal(10.5, result.Location.Coordinates.Latitude);
        Assert.Equal("Fictional Events Co", result.Organizer?.Name);
        Assert.Equal(["music", "evening"], result.Tags);
        Assert.Equal("Tickets", Assert.Single(result.Links).Label);
    }

    [Fact]
    public async Task GetEventDetailAsync_ReturnsNull_OnNonSuccessStatus()
    {
        Assert.Null(await CreateSut(FakeApi.Respond(HttpStatusCode.NotFound)).GetEventDetailAsync("evt-missing"));
    }

    // ── UpdateEventAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateEventAsync_PutsRequestToEditEndpoint()
    {
        StubHttpMessageHandler handler = FakeApi.Respond(HttpStatusCode.OK, DetailJson);
        UpdateEventRequest request = new() { Id = "evt/030", EventName = "Silver Birch Concert", Status = "confirmed", ExpectedAttendees = 300 };

        await CreateSut(handler).UpdateEventAsync(request);

        Assert.Equal(HttpMethod.Put, handler.LastRequest.Method);
        Assert.Equal("events/edit", handler.LastRequest.RelativeUrl);
        using JsonDocument body = JsonDocument.Parse(handler.LastRequest.Body!);
        Assert.Equal("evt/030", body.RootElement.GetProperty("id").GetString());
        Assert.Equal("confirmed", body.RootElement.GetProperty("status").GetString());
        Assert.Equal(300, body.RootElement.GetProperty("expectedAttendees").GetInt32());
    }

    [Fact]
    public async Task UpdateEventAsync_ReturnsUpdatedDetail_OnSuccess()
    {
        EventDetail? result = await CreateSut(FakeApi.Respond(HttpStatusCode.OK, DetailJson))
            .UpdateEventAsync(new UpdateEventRequest { Id = "evt/030" });

        Assert.NotNull(result);
        Assert.Equal("Silver Birch Concert", result.EventName);
        Assert.Equal("BK-0030", result.BookingReference);
    }

    [Fact]
    public async Task UpdateEventAsync_ReturnsNull_OnNonSuccessStatus()
    {
        Assert.Null(await CreateSut(FakeApi.Respond(HttpStatusCode.Conflict)).UpdateEventAsync(new UpdateEventRequest()));
    }
}
