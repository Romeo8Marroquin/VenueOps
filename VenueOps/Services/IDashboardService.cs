using VenueOps.Models.Common;
using VenueOps.Models.Dashboard;
using VenueOps.Models.Events;

namespace VenueOps.Services;

public interface IDashboardService
{
    Task<DashboardOverview?> GetOverviewAsync(CancellationToken ct = default);

    Task<PagedResult<RecentEvent>?> GetRecentEventsAsync(
        int page, int pageSize, CancellationToken ct = default);
}
