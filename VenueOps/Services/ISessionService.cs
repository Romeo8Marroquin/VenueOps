using VenueOps.Models;

namespace VenueOps.Services;

public interface ISessionService
{
    UserInfo? CurrentUser { get; set; }
}
