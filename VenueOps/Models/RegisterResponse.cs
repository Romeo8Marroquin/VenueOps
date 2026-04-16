using System.Text.Json.Serialization;

namespace VenueOps.Models;

public sealed class RegisterResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    // No token — caller must sign in after registration.
    [JsonPropertyName("user")]
    public UserInfo? User { get; set; }
}
