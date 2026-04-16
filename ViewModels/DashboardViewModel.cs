using CommunityToolkit.Mvvm.Input;
using VenueOps.Services;

namespace VenueOps.ViewModels;

public partial class DashboardViewModel : BaseViewModel
{
    private readonly INavigationService _navigationService;

    public DashboardViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
        Title = "Dashboard";
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            // TODO: Clear stored auth token / session here when persistence is added.
            await _navigationService.NavigateToLoginAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    protected override void OnBusyStateChanged(bool isBusy)
        => LogoutCommand.NotifyCanExecuteChanged();
}
