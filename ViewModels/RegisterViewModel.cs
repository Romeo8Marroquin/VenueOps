using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VenueOps.Models;
using VenueOps.Services;

namespace VenueOps.ViewModels;

public partial class RegisterViewModel : BaseViewModel
{
    private readonly IAuthService _authService;
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;

    public RegisterViewModel(
        IAuthService authService,
        INavigationService navigationService,
        IDialogService dialogService)
    {
        _authService    = authService;
        _navigationService = navigationService;
        _dialogService  = dialogService;
        Title = "Create Account";
    }

    // ── Bindable properties ─────────────────────────────────────────────────

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Email { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Password { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string PasswordConfirmation { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string ErrorMessage { get; set; } = string.Empty;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    // ── Commands ────────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task RegisterAsync()
    {
        if (IsBusy) return;

        // ── Local validation ─────────────────────────────────────────────────
        if (string.IsNullOrWhiteSpace(Name)  || string.IsNullOrWhiteSpace(Email) ||
            string.IsNullOrWhiteSpace(Password) || string.IsNullOrWhiteSpace(PasswordConfirmation))
        {
            ErrorMessage = "Please fill in all fields.";
            return;
        }

        if (Password != PasswordConfirmation)
        {
            ErrorMessage = "Passwords do not match.";
            return;
        }

        IsBusy = true;
        ErrorMessage = string.Empty;

        try
        {
            var request = new RegisterRequest
            {
                Name                 = Name,
                Email                = Email,
                Password             = Password,
                PasswordConfirmation = PasswordConfirmation
            };

            var response = await _authService.RegisterAsync(request);

            if (response is not null)
            {
                Password             = string.Empty;
                PasswordConfirmation = string.Empty;

                IsBusy = false; // stop spinner before dialog
                await _dialogService.ShowAlertAsync(
                    "Account created",
                    $"Welcome, {response.User!.Name}! You can now sign in.",
                    "Sign in");

                await _navigationService.NavigateToLoginAsync();
            }
            else
            {
                IsBusy = false;
                await _dialogService.ShowAlertAsync(
                    "Registration failed",
                    "We couldn't create your account. Please try again.",
                    "OK");
            }
        }
        catch (HttpRequestException)
        {
            IsBusy = false;
            await _dialogService.ShowAlertAsync(
                "Connection error",
                "Unable to connect. Please check your connection and try again.",
                "OK");
        }
        catch (Exception)
        {
            IsBusy = false;
            await _dialogService.ShowAlertAsync(
                "Unexpected error",
                "Something went wrong. Please try again.",
                "OK");
        }
        finally
        {
            IsBusy = false; // safety net
        }
    }

    [RelayCommand]
    private Task NavigateToLoginAsync() => _navigationService.NavigateToLoginAsync();

    protected override void OnBusyStateChanged(bool isBusy)
        => RegisterCommand.NotifyCanExecuteChanged();
}
