using CommunityToolkit.Mvvm.Input;
using VenueOps.Services;

namespace VenueOps.ViewModels;

/// <summary>
/// ViewModel for the AppShell flyout footer.
/// Singleton — lives for the entire authenticated session.
/// </summary>
public partial class AppShellViewModel : BaseViewModel
{
    private readonly INavigationService _navigationService;
    private readonly ISessionService _sessionService;

    public AppShellViewModel(INavigationService navigationService, ISessionService sessionService)
    {
        _navigationService = navigationService;
        _sessionService    = sessionService;
        _sessionService.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ISessionService.CurrentUser))
            {
                OnPropertyChanged(nameof(UserName));
                OnPropertyChanged(nameof(ShortEmail));
                OnPropertyChanged(nameof(UserInitial));
            }
        };
    }

    /// <summary>Display name shown in the flyout footer.</summary>
    public string UserName => _sessionService.CurrentUser?.Name ?? "User";

    /// <summary>
    /// Shows only the local part before @ to keep the narrow flyout tidy.
    /// e.g. "romeo@gmail.com" → "romeo"
    /// </summary>
    public string ShortEmail
    {
        get
        {
            var email   = _sessionService.CurrentUser?.Email ?? string.Empty;
            var atIndex = email.IndexOf('@');
            return atIndex > 0 ? email[..atIndex] : email;
        }
    }

    /// <summary>
    /// First letter of the user's name, uppercased — shown in the avatar circle.
    /// e.g. "Demo User" → "D"
    /// </summary>
    public string UserInitial
    {
        get
        {
            var name = _sessionService.CurrentUser?.Name ?? "U";
            return name.Length > 0 ? name[0].ToString().ToUpperInvariant() : "U";
        }
    }

    [RelayCommand]
    private async Task SignOutAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            _sessionService.CurrentUser = null;
            await _navigationService.NavigateToLoginAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    protected override void OnBusyStateChanged(bool isBusy)
        => SignOutCommand.NotifyCanExecuteChanged();
}
