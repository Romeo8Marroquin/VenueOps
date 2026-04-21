using System.Net;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VenueOps.Models;
using VenueOps.Services;

namespace VenueOps.ViewModels;

public partial class LoginViewModel : BaseViewModel
{
    private readonly IAuthService _authService;
    private readonly INavigationService _navigationService;
    private readonly ISessionService _sessionService;

    public LoginViewModel(IAuthService authService, INavigationService navigationService, ISessionService sessionService)
    {
        _authService       = authService;
        _navigationService = navigationService;
        _sessionService    = sessionService;
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

    private bool CanLogin() => !IsBusy;

    [RelayCommand(CanExecute = nameof(CanLogin))]
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

            _sessionService.CurrentUser = response.User; // store for flyout footer
            Password = string.Empty;                     // clear sensitive field
            await _navigationService.NavigateToMainAsync();
        }
        catch (AuthException ex)
        {
            ErrorMessage = ex.StatusCode switch
            {
                HttpStatusCode.Unauthorized          => "Invalid email or password.",
                HttpStatusCode.Forbidden             => "Access denied.",
                >= HttpStatusCode.InternalServerError => "Server error. Please try again later.",
                _                                    => "Sign-in failed. Please try again."
            };
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

    protected override void OnBusyStateChanged(bool isBusy)
        => LoginCommand.NotifyCanExecuteChanged();
}
