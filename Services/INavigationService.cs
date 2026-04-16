namespace VenueOps.Services;

public interface INavigationService
{
    /// <summary>Switches Application.MainPage to AppShell (authenticated area).</summary>
    Task NavigateToMainAsync();

    /// <summary>Switches Application.MainPage to LoginView (unauthenticated area).</summary>
    Task NavigateToLoginAsync();
}
