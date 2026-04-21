using System.Text.Json.Serialization;

namespace VenueOps.Models.Dashboard;

public sealed class DashboardOverview
{
    [JsonPropertyName("insight")]
    public InsightInfo Insight { get; set; } = new();

    [JsonPropertyName("stats")]
    public DashboardStats Stats { get; set; } = new();
}
