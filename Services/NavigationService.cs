using VenueOps.Views.Auth;

namespace VenueOps.Services;

/// <summary>
/// Handles root-level page switches (Login ↔ Authenticated shell).
/// Keeps ViewModels free of direct Application / Window / Shell references.
///
/// Uses Window.Page (MAUI 9+ pattern) instead of the deprecated
/// Application.MainPage.
/// </summary>
public sealed class NavigationService(IServiceProvider serviceProvider) : INavigationService
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    public Task NavigateToMainAsync()
    {
        // AppShell is Singleton — reuses the same instance for the session.
        Application.Current!.Windows[0].Page = _serviceProvider.GetRequiredService<AppShell>();
        return Task.CompletedTask;
    }

    public Task NavigateToLoginAsync()
    {
        // LoginView is Transient — produces a fresh ViewModel on every call.
        Application.Current!.Windows[0].Page = _serviceProvider.GetRequiredService<LoginView>();
        return Task.CompletedTask;
    }
}
