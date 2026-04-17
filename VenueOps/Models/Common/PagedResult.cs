using System.Text.Json.Serialization;

namespace VenueOps.Models.Common;

/// <summary>
/// Generic paginated API response. Reused across any endpoint that returns
/// a paged list (events, bookings, venues, etc.).
/// </summary>
public sealed class PagedResult<T>
{
    [JsonPropertyName("items")]
    public List<T> Items { get; set; } = [];

    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; }

    [JsonPropertyName("hasMore")]
    public bool HasMore { get; set; }
}
