using VenueOps.Models;

namespace VenueOps.Services;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken ct = default);

    Task<RegisterResponse?> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
}
