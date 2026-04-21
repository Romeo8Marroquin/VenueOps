using System.ComponentModel;
using VenueOps.Models;

namespace VenueOps.Services;

public interface ISessionService : INotifyPropertyChanged
{
    UserInfo? CurrentUser { get; set; }
}
