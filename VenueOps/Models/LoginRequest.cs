using System.Text.Json.Serialization;

namespace VenueOps.Models;

public sealed class LoginRequest
{
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;
}
