namespace VenueOps.Services;

public interface INavigationService
{
    /// <summary>Switches Window[0].Page to AppShell (authenticated area).</summary>
    Task NavigateToMainAsync();

    /// <summary>Switches Window[0].Page to LoginView (unauthenticated area).</summary>
    Task NavigateToLoginAsync();

    /// <summary>Switches Window[0].Page to RegisterView (sign-up flow).</summary>
    Task NavigateToRegisterAsync();

    /// <summary>
    /// Pushes CreateEventView as a modal over the current Shell page.
    /// <paramref name="onCreated"/> is called (from the UI thread) after the modal
    /// is fully dismissed so the caller can refresh its data without race conditions.
    /// </summary>
    Task PushCreateEventModalAsync(Func<Task> onCreated);
}
