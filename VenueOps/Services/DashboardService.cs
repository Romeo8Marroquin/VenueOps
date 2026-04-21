using System.Net.Http.Json;
using VenueOps.Models.Common;
using VenueOps.Models.Dashboard;
using VenueOps.Models.Events;

namespace VenueOps.Services;

public sealed class DashboardService(IHttpClientFactory factory) : IDashboardService
{
    private readonly HttpClient _http = factory.CreateClient("VenueOpsApi");

    public async Task<DashboardOverview?> GetOverviewAsync(CancellationToken ct = default)
    {
        var response = await _http.GetAsync("dashboard/overview", ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<DashboardOverview>(ct);
    }

    public async Task<PagedResult<RecentEvent>?> GetRecentEventsAsync(
        int page, int pageSize,
        string? query = null, string? status = null,
        CancellationToken ct = default)
    {
        var q = Uri.EscapeDataString(query ?? string.Empty);
        var s = string.IsNullOrWhiteSpace(status) ? "all" : status;
        var url = $"events/recent?query={q}&status={s}&venueUuid=&page={page}&pageSize={pageSize}&sortBy=startDateUtc&sortDirection=desc";
        var response = await _http.GetAsync(url, ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<PagedResult<RecentEvent>>(ct);
    }
}
