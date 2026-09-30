using VenueOps.Models;
using VenueOps.Models.Common;
using VenueOps.Models.Dashboard;
using VenueOps.Models.Events;

namespace VenueOps.Tests.Models;

/// <summary>
/// Default values the view models and services rely on: strings start empty (never null),
/// collections start empty, and new create/update requests default to the "draft" status.
/// </summary>
public class ModelDefaultsTests
{
    [Fact]
    public void AuthModels_DefaultToEmptyStringsAndNoUser()
    {
        LoginRequest login = new();
        LoginResponse loginResponse = new();
        RegisterRequest register = new();
        RegisterResponse registerResponse = new();
        UserInfo user = new();

        Assert.Equal(string.Empty, login.Email);
        Assert.Equal(string.Empty, login.Password);
        Assert.False(loginResponse.Success);
        Assert.Equal(string.Empty, loginResponse.Token);
        Assert.Null(loginResponse.User);
        Assert.Equal(string.Empty, register.Name);
        Assert.Equal(string.Empty, register.Email);
        Assert.Equal(string.Empty, register.Password);
        Assert.Equal(string.Empty, register.PasswordConfirmation);
        Assert.False(registerResponse.Success);
        Assert.Null(registerResponse.User);
        Assert.Equal(string.Empty, user.Uuid);
        Assert.Equal(string.Empty, user.Name);
        Assert.Equal(string.Empty, user.Email);
        Assert.Equal(string.Empty, user.Role);
    }

    [Fact]
    public void PagedResult_DefaultsToEmptyFirstState()
    {
        PagedResult<RecentEvent> result = new();

        Assert.Empty(result.Items);
        Assert.Equal(0, result.Total);
        Assert.Equal(0, result.Page);
        Assert.Equal(0, result.PageSize);
        Assert.False(result.HasMore);
    }

    [Fact]
    public void DashboardOverview_DefaultsToNonNullInsightAndStats()
    {
        DashboardOverview overview = new();

        Assert.Equal(string.Empty, overview.Insight.Message);
        Assert.Equal(default, overview.Insight.LastUpdatedUtc);
        Assert.Equal(0, overview.Stats.TotalEvents);
        Assert.Equal(0, overview.Stats.TotalBookings);
        Assert.Equal(0, overview.Stats.TotalAttendees);
        Assert.Equal(0, overview.Stats.ActiveVenues);
        Assert.Equal(string.Empty, overview.Stats.OccupancyRate);
        Assert.Empty(overview.Stats.Sponsors);
    }

    [Fact]
    public void CreateEventRequest_DefaultsToDraftWithOptionalSectionsUnset()
    {
        CreateEventRequest request = new();

        Assert.Equal("draft", request.Status);
        Assert.Equal(string.Empty, request.EventName);
        Assert.Equal(string.Empty, request.VenueName);
        Assert.Equal(0, request.ExpectedAtendees);
        Assert.Null(request.ShortDescription);
        Assert.Null(request.Location);
        Assert.Null(request.Organizer);
        Assert.Null(request.Tags);
        Assert.Null(request.Links);
        Assert.Null(request.Images);
    }

    [Fact]
    public void UpdateEventRequest_DefaultsToDraftWithOptionalSectionsUnset()
    {
        UpdateEventRequest request = new();

        Assert.Equal("draft", request.Status);
        Assert.Equal(string.Empty, request.Id);
        Assert.Equal(string.Empty, request.EventName);
        Assert.Equal(string.Empty, request.VenueName);
        Assert.Equal(0, request.ExpectedAttendees);
        Assert.Equal(0, request.AttendeeCount);
        Assert.Null(request.ShortDescription);
        Assert.Null(request.Location);
        Assert.Null(request.Organizer);
        Assert.Null(request.Tags);
        Assert.Null(request.Links);
        Assert.Null(request.Images);
    }

    [Fact]
    public void EventDetail_DefaultsToEmptyCollectionsAndNoOptionalSections()
    {
        EventDetail detail = new();

        Assert.Equal(string.Empty, detail.Id);
        Assert.Equal(string.Empty, detail.Status);
        Assert.Equal(string.Empty, detail.ShortDescription);
        Assert.Equal(string.Empty, detail.BookingReference);
        Assert.Empty(detail.Images);
        Assert.Empty(detail.Tags);
        Assert.Empty(detail.Links);
        Assert.Null(detail.Location);
        Assert.Null(detail.Organizer);
    }

    [Fact]
    public void EventDetailParts_DefaultToEmptyStrings()
    {
        Assert.Equal(string.Empty, new EventImage().Url);
        Assert.Equal(string.Empty, new EventImage().AltText);
        Assert.Equal(string.Empty, new EventImage().Credit);
        Assert.Equal(string.Empty, new EventLocation().FormattedAddress);
        Assert.Null(new EventLocation().Coordinates);
        Assert.Equal(string.Empty, new EventOrganizer().Name);
        Assert.Null(new EventOrganizer().WebsiteUrl);
        Assert.Equal(string.Empty, new EventLink().Label);
        Assert.Equal(string.Empty, new EventLink().Url);
    }

    [Fact]
    public void EventSummaries_DefaultToEmptyStrings()
    {
        RecentEvent recent = new();
        CreateEventResponse created = new();

        Assert.Equal(string.Empty, recent.Id);
        Assert.Equal(string.Empty, recent.EventName);
        Assert.Equal(string.Empty, recent.VenueName);
        Assert.Equal(string.Empty, recent.Status);
        Assert.Equal(string.Empty, recent.BookingReference);
        Assert.Equal(string.Empty, created.Id);
        Assert.Equal(string.Empty, created.EventName);
        Assert.Equal(string.Empty, created.VenueName);
        Assert.Equal(string.Empty, created.Status);
        Assert.Equal(string.Empty, created.BookingReference);
    }
}
