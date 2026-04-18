using VenueOps.Models.Common;
using VenueOps.Models.Events;

namespace VenueOps.Services;

public interface IEventsService
{
    Task<PagedResult<RecentEvent>?> GetEventsAsync(
        int page, int pageSize,
        string? query = null, string? status = null,
        string sortBy = "startDateUtc", string sortDirection = "desc",
        CancellationToken ct = default);
}
