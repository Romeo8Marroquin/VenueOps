using VenueOps.Models;

namespace VenueOps.Services;

/// <summary>
/// Holds the currently authenticated user for the session.
/// Populated by LoginViewModel after a successful sign-in and
/// cleared by AppShellViewModel on sign-out.
/// </summary>
public sealed class SessionService : ISessionService
{
    public UserInfo? CurrentUser { get; set; }
}
