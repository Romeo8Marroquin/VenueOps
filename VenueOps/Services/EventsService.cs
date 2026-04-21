using System.Net.Http.Json;
using VenueOps.Models.Common;
using VenueOps.Models.Events;

namespace VenueOps.Services;

public sealed class EventsService(IHttpClientFactory factory) : IEventsService
{
    private readonly HttpClient _http = factory.CreateClient("VenueOpsApi");

    public async Task<PagedResult<RecentEvent>?> GetEventsAsync(
        int page, int pageSize,
        string? query = null, string? status = null,
        string sortBy = "startDateUtc", string sortDirection = "desc",
        CancellationToken ct = default)
    {
        var q = Uri.EscapeDataString(query ?? string.Empty);
        var s = string.IsNullOrWhiteSpace(status) ? "all" : status;
        var url = $"events/list?query={q}&status={s}&venueUuid=&page={page}&pageSize={pageSize}&sortBy={sortBy}&sortDirection={sortDirection}";
        var response = await _http.GetAsync(url, ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<PagedResult<RecentEvent>>(ct);
    }

    public async Task<CreateEventResponse?> CreateEventAsync(
        CreateEventRequest request,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("events/new", request, ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<CreateEventResponse>(ct);
    }

    public async Task<EventDetail?> GetEventDetailAsync(
        string eventId,
        CancellationToken ct = default)
    {
        var encoded = Uri.EscapeDataString(eventId);
        var response = await _http.GetAsync($"events/detail?id={encoded}", ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<EventDetail>(ct);
    }
}
