using VenueOps.Models.Events;

namespace VenueOps.Tests.Models;

/// <summary>
/// <see cref="RecentEvent.IsImportant"/> is derived in the app from the attendee count alone:
/// 500 or more attendees is important, whatever the status or the dates.
/// </summary>
public class RecentEventTests
{
    private static readonly string[] Statuses = ["confirmed", "draft", "completed", "cancelled"];

    private static readonly DateTime Past = new(2020, 3, 1, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Future = new(2090, 3, 1, 9, 0, 0, DateTimeKind.Utc);

    public static TheoryData<string, DateTime, DateTime> StatusAndDateCombinations
    {
        get
        {
            TheoryData<string, DateTime, DateTime> data = new();
            foreach (string status in Statuses)
            {
                data.Add(status, Past, Past.AddHours(3));
                data.Add(status, Future, Future.AddHours(3));
                data.Add(status, Past, Future);
            }

            return data;
        }
    }

    public static TheoryData<string> AllStatuses
    {
        get
        {
            TheoryData<string> data = new();
            foreach (string status in Statuses)
            {
                data.Add(status);
            }

            return data;
        }
    }

    private static RecentEvent CreateEvent(int attendeeCount, string status = "confirmed") => new()
    {
        Id = "evt-1700",
        EventName = "Lantern Walk",
        VenueName = "Old Mill",
        Status = status,
        StartDateUtc = Future,
        EndDateUtc = Future.AddHours(3),
        AttendeeCount = attendeeCount,
        BookingReference = "BK-1700"
    };

    [Theory]
    [InlineData(500)]
    [InlineData(501)]
    [InlineData(10000)]
    [InlineData(int.MaxValue)]
    public void IsImportant_IsTrue_WhenAttendeeCountIsAtLeastThreshold(int attendeeCount)
    {
        RecentEvent recentEvent = CreateEvent(attendeeCount);

        Assert.True(recentEvent.IsImportant);
    }

    [Theory]
    [MemberData(nameof(StatusAndDateCombinations))]
    public void IsImportant_IsTrue_ForThresholdEventWhateverTheStatusOrDates(string status, DateTime start, DateTime end)
    {
        RecentEvent recentEvent = CreateEvent(500, status);
        recentEvent.StartDateUtc = start;
        recentEvent.EndDateUtc = end;

        Assert.True(recentEvent.IsImportant);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(499)]
    public void IsImportant_IsFalse_WhenAttendeeCountIsBelowThreshold(int attendeeCount)
    {
        RecentEvent recentEvent = CreateEvent(attendeeCount);

        Assert.False(recentEvent.IsImportant);
    }

    [Theory]
    [MemberData(nameof(AllStatuses))]
    public void IsImportant_IsFalse_ForSmallerEventInAnyStatus(string status)
    {
        RecentEvent recentEvent = CreateEvent(499, status);

        Assert.False(recentEvent.IsImportant);
    }

    [Fact]
    public void IsImportant_IsFalseByDefault_AndIsComputedFromAttendeeCount()
    {
        RecentEvent recentEvent = new();

        Assert.False(recentEvent.IsImportant);

        recentEvent.AttendeeCount = 499;
        Assert.False(recentEvent.IsImportant);

        recentEvent.AttendeeCount = 500;
        Assert.True(recentEvent.IsImportant);

        recentEvent.AttendeeCount = 499;
        Assert.False(recentEvent.IsImportant);
    }
}
