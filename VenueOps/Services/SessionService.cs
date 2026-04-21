using CommunityToolkit.Mvvm.ComponentModel;
using VenueOps.Models;

namespace VenueOps.Services;

public sealed partial class SessionService : ObservableObject, ISessionService
{
    [ObservableProperty]
    public partial UserInfo? CurrentUser { get; set; }
}
