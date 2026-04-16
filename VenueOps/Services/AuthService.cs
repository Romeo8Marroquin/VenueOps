using System.Net.Http.Json;
using VenueOps.Models;

namespace VenueOps.Services;

public sealed class AuthService(IHttpClientFactory factory) : IAuthService
{
    private readonly HttpClient _http = factory.CreateClient("VenueOpsApi");

    public async Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var httpResponse = await _http.PostAsJsonAsync("auth/sign-in", request, ct);

        if (!httpResponse.IsSuccessStatusCode)
            return null;

        var result = await httpResponse.Content.ReadFromJsonAsync<LoginResponse>(ct);

        // Only return a result when the API also reports success in the body
        return result?.Success == true ? result : null;
    }

    public async Task<RegisterResponse?> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var httpResponse = await _http.PostAsJsonAsync("auth/sign-up", request, ct);

        if (!httpResponse.IsSuccessStatusCode)
            return null;

        var result = await httpResponse.Content.ReadFromJsonAsync<RegisterResponse>(ct);

        return result?.Success == true ? result : null;
    }
}
