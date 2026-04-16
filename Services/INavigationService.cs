namespace VenueOps.Services;

public interface INavigationService
{
    /// <summary>Switches Window[0].Page to AppShell (authenticated area).</summary>
    Task NavigateToMainAsync();

    /// <summary>Switches Window[0].Page to LoginView (unauthenticated area).</summary>
    Task NavigateToLoginAsync();

    /// <summary>Switches Window[0].Page to RegisterView (sign-up flow).</summary>
    Task NavigateToRegisterAsync();
}
