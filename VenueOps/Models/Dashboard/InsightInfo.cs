using System.Text.Json.Serialization;

namespace VenueOps.Models.Dashboard;

public sealed class InsightInfo
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>UTC timestamp — convert to device local time before display.</summary>
    [JsonPropertyName("lastUpdatedUtc")]
    public DateTime LastUpdatedUtc { get; set; }
}
