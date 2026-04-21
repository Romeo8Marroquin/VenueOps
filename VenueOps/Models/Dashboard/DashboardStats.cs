using System.Text.Json.Serialization;

namespace VenueOps.Models.Dashboard;

public sealed class DashboardStats
{
    [JsonPropertyName("totalEvents")]
    public int TotalEvents { get; set; }

    [JsonPropertyName("totalBookings")]
    public int TotalBookings { get; set; }

    [JsonPropertyName("totalAttendees")]
    public int TotalAttendees { get; set; }

    [JsonPropertyName("activeVenues")]
    public int ActiveVenues { get; set; }

    /// <summary>Formatted percentage string — e.g. "78%".</summary>
    [JsonPropertyName("occupancyRate")]
    public string OccupancyRate { get; set; } = string.Empty;

    [JsonPropertyName("sponsors")]
    public List<string> Sponsors { get; set; } = [];
}
