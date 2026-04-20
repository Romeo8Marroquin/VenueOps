using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Extensions;
using VenueOps.ViewModels;
using VenueOps.Views.Auth;
using VenueOps.Views.Events;

namespace VenueOps.Services;

/// <summary>
/// Handles root-level page switches (Login ↔ Register ↔ Authenticated shell).
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
        // AppShell is Singleton — reuses the same instance across sign-ins.
        Application.Current!.Windows[0].Page = _serviceProvider.GetRequiredService<AppShell>();
        // Refresh the flyout footer so it shows the newly signed-in user's info.
        _serviceProvider.GetRequiredService<AppShellViewModel>().RefreshUserInfo();
        return Task.CompletedTask;
    }

    public Task NavigateToLoginAsync()
    {
        // LoginView is Transient — produces a fresh ViewModel on every call.
        Application.Current!.Windows[0].Page = _serviceProvider.GetRequiredService<LoginView>();
        return Task.CompletedTask;
    }

    public Task NavigateToRegisterAsync()
    {
        // RegisterView is Transient — produces a fresh ViewModel on every call.
        Application.Current!.Windows[0].Page = _serviceProvider.GetRequiredService<RegisterView>();
        return Task.CompletedTask;
    }

    public async Task PushCreateEventModalAsync(Func<Task> onCreated)
    {
        var popup = _serviceProvider.GetRequiredService<CreateEventView>();
        Page? host  = Application.Current?.Windows[0].Page;
        if (host is null) return;

        // ShowPopupAsync returns the value passed to CloseAsync():
        //   true  → event was created → refresh caller
        //   null  → cancelled / dismissed → no refresh
        PopupOptions options = new()
        {
            CanBeDismissedByTappingOutsideOfPopup = true,
            PageOverlayColor = Colors.Black.WithAlpha(0.45f),
            Shape = null,
            Shadow = null
        };
        IPopupResult<bool> result = await host.ShowPopupAsync<bool>(popup, options);

        if (!result.WasDismissedByTappingOutsideOfPopup && result.Result is true)
            await onCreated();
    }
}
