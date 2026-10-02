using VenueOps.Services;

namespace VenueOps.Tests.Services;

public class EventsListPreferencesServiceTests
{
    [Fact]
    public void UpcomingOnly_IsOff_ByDefault()
    {
        Assert.False(new EventsListPreferencesService().UpcomingOnly);
    }

    [Fact]
    public void UpcomingOnly_ReturnsTrue_AfterItIsTurnedOn()
    {
        EventsListPreferencesService sut = new();

        sut.UpcomingOnly = true;

        Assert.True(sut.UpcomingOnly);
    }

    [Fact]
    public void UpcomingOnly_ReturnsFalse_AfterItIsTurnedOnAndOffAgain()
    {
        EventsListPreferencesService sut = new() { UpcomingOnly = true };

        sut.UpcomingOnly = false;

        Assert.False(sut.UpcomingOnly);
    }

    [Fact]
    public void UpcomingOnly_IsSharedBetweenConsumersOfTheSameInstance()
    {
        IEventsListPreferencesService shared = new EventsListPreferencesService();
        IEventsListPreferencesService writer = shared;
        IEventsListPreferencesService reader = shared;

        writer.UpcomingOnly = true;

        Assert.True(reader.UpcomingOnly);
    }

    [Fact]
    public void UpcomingOnly_IsNotSharedBetweenSeparateInstances()
    {
        EventsListPreferencesService first = new() { UpcomingOnly = true };

        Assert.False(new EventsListPreferencesService().UpcomingOnly);
        Assert.True(first.UpcomingOnly);
    }
}
