using VenueOps.Models;

namespace VenueOps.Services;

public interface IAuthService
{
    /// <exception cref="AuthException">Thrown when the server rejects the request or returns success:false.</exception>
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);

    /// <exception cref="AuthException">Thrown when the server rejects the request or returns success:false.</exception>
    Task<RegisterResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
}
