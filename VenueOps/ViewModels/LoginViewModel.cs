using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VenueOps.Models;
using VenueOps.Services;

namespace VenueOps.ViewModels;

public partial class LoginViewModel : BaseViewModel
{
    private readonly IAuthService _authService;
    private readonly INavigationService _navigationService;

    public LoginViewModel(IAuthService authService, INavigationService navigationService)
    {
        _authService = authService;
        _navigationService = navigationService;
        Title = "Sign In";
    }

    // ── Bindable properties ─────────────────────────────────────────────────

    [ObservableProperty]
    public partial string Email { get; set; } = string.Empty;

    // Storing password as a plain string is acceptable for a POC.
    // In production, clear this field immediately after the request completes.
    [ObservableProperty]
    public partial string Password { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string ErrorMessage { get; set; } = string.Empty;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    // ── Command ─────────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (IsBusy) return;

        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Please enter your email and password.";
            return;
        }

        IsBusy = true;
        ErrorMessage = string.Empty;

        try
        {
            var request = new LoginRequest { Email = Email, Password = Password };
            var response = await _authService.LoginAsync(request);

            if (response is not null)
            {
                Password = string.Empty; // clear sensitive field before navigation
                await _navigationService.NavigateToMainAsync();
            }
            else
            {
                ErrorMessage = "Invalid credentials. Please try again.";
            }
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Unable to connect. Please check your connection.";
        }
        catch (Exception)
        {
            ErrorMessage = "An unexpected error occurred. Please try again.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    // ── Navigation ──────────────────────────────────────────────────────────

    [RelayCommand]
    private Task NavigateToRegisterAsync() => _navigationService.NavigateToRegisterAsync();

    // Notify the login command's CanExecute when IsBusy flips
    protected override void OnBusyStateChanged(bool isBusy)
        => LoginCommand.NotifyCanExecuteChanged();
}
