using System.Net;
using Moq;
using VenueOps.Models.Common;
using VenueOps.Models.Dashboard;
using VenueOps.Models.Events;
using VenueOps.Services;
using VenueOps.Tests.TestDoubles;

namespace VenueOps.Tests.Services;

public class DashboardServiceTests
{
    private const string OverviewJson =
        """
        {
          "insight": { "message": "Bookings are up this week.", "lastUpdatedUtc": "2030-04-01T08:30:00Z" },
          "stats": {
            "totalEvents": 12, "totalBookings": 34, "totalAttendees": 560, "activeVenues": 3,
            "occupancyRate": "78%", "sponsors": ["Fictional Sponsor A", "Fictional Sponsor B"]
          }
        }
        """;

    private const string RecentEventsJson =
        """
        {
          "items": [
            { "id": "evt-001", "eventName": "Harbor Lights Gala", "venueName": "Pier Hall",
              "status": "confirmed", "startDateUtc": "2030-05-01T18:00:00Z", "endDateUtc": "2030-05-01T22:00:00Z",
              "attendeeCount": 150, "bookingReference": "BK-0001" }
          ],
          "total": 21, "page": 2, "pageSize": 5, "hasMore": true
        }
        """;

    private static DashboardService CreateSut(StubHttpMessageHandler handler) => new(FakeApi.CreateFactory(handler).Object);

    [Fact]
    public void Constructor_CreatesNamedApiClient()
    {
        Mock<IHttpClientFactory> factory = FakeApi.CreateFactory(FakeApi.Respond(HttpStatusCode.OK));

        _ = new DashboardService(factory.Object);

        factory.Verify(f => f.CreateClient(FakeApi.ClientName), Times.Once);
    }

    // ── GetOverviewAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetOverviewAsync_GetsOverviewEndpointAndDeserializes_OnSuccess()
    {
        StubHttpMessageHandler handler = FakeApi.Respond(HttpStatusCode.OK, OverviewJson);

        DashboardOverview? result = await CreateSut(handler).GetOverviewAsync();

        Assert.Equal(HttpMethod.Get, handler.LastRequest.Method);
        Assert.Equal("dashboard/overview", handler.LastRequest.RelativeUrl);
        Assert.NotNull(result);
        Assert.Equal("Bookings are up this week.", result.Insight.Message);
        Assert.Equal(new DateTime(2030, 4, 1, 8, 30, 0, DateTimeKind.Utc), result.Insight.LastUpdatedUtc);
        Assert.Equal(12, result.Stats.TotalEvents);
        Assert.Equal(34, result.Stats.TotalBookings);
        Assert.Equal(560, result.Stats.TotalAttendees);
        Assert.Equal(3, result.Stats.ActiveVenues);
        Assert.Equal("78%", result.Stats.OccupancyRate);
        Assert.Equal(["Fictional Sponsor A", "Fictional Sponsor B"], result.Stats.Sponsors);
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task GetOverviewAsync_ReturnsNull_OnNonSuccessStatus(HttpStatusCode code)
    {
        Assert.Null(await CreateSut(FakeApi.Respond(code)).GetOverviewAsync());
    }

    // ── GetRecentEventsAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task GetRecentEventsAsync_UsesEmptyQueryAndAllStatus_WhenFiltersAreNull()
    {
        StubHttpMessageHandler handler = FakeApi.Respond(HttpStatusCode.OK, RecentEventsJson);

        await CreateSut(handler).GetRecentEventsAsync(1, 5);

        Assert.Equal(HttpMethod.Get, handler.LastRequest.Method);
        Assert.Equal(
            "events/recent?query=&status=all&venueUuid=&page=1&pageSize=5&sortBy=startDateUtc&sortDirection=desc",
            handler.LastRequest.RelativeUrl);
    }

    [Fact]
    public async Task GetRecentEventsAsync_EscapesQueryAndPassesStatus_WhenFiltersAreProvided()
    {
        StubHttpMessageHandler handler = FakeApi.Respond(HttpStatusCode.OK, RecentEventsJson);

        await CreateSut(handler).GetRecentEventsAsync(3, 10, "jazz & blues", "confirmed");

        Assert.Equal(
            "events/recent?query=jazz%20%26%20blues&status=confirmed&venueUuid=&page=3&pageSize=10&sortBy=startDateUtc&sortDirection=desc",
            handler.LastRequest.RelativeUrl);
    }

    [Fact]
    public async Task GetRecentEventsAsync_FallsBackToAllStatus_WhenStatusIsWhitespace()
    {
        StubHttpMessageHandler handler = FakeApi.Respond(HttpStatusCode.OK, RecentEventsJson);

        await CreateSut(handler).GetRecentEventsAsync(1, 5, status: "   ");

        Assert.Contains("status=all&", handler.LastRequest.RelativeUrl);
    }

    [Fact]
    public async Task GetRecentEventsAsync_DeserializesPagedResult_OnSuccess()
    {
        PagedResult<RecentEvent>? result = await CreateSut(FakeApi.Respond(HttpStatusCode.OK, RecentEventsJson))
            .GetRecentEventsAsync(2, 5);

        Assert.NotNull(result);
        Assert.Equal(21, result.Total);
        Assert.Equal(2, result.Page);
        Assert.Equal(5, result.PageSize);
        Assert.True(result.HasMore);
        RecentEvent evt = Assert.Single(result.Items);
        Assert.Equal("evt-001", evt.Id);
        Assert.Equal("Harbor Lights Gala", evt.EventName);
        Assert.Equal("Pier Hall", evt.VenueName);
        Assert.Equal("confirmed", evt.Status);
        Assert.Equal(new DateTime(2030, 5, 1, 18, 0, 0, DateTimeKind.Utc), evt.StartDateUtc);
        Assert.Equal(new DateTime(2030, 5, 1, 22, 0, 0, DateTimeKind.Utc), evt.EndDateUtc);
        Assert.Equal(150, evt.AttendeeCount);
        Assert.Equal("BK-0001", evt.BookingReference);
    }

    [Fact]
    public async Task GetRecentEventsAsync_ReturnsNull_OnNonSuccessStatus()
    {
        Assert.Null(await CreateSut(FakeApi.Respond(HttpStatusCode.BadGateway)).GetRecentEventsAsync(1, 5));
    }
}
