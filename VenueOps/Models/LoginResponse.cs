using System.Text.Json.Serialization;

namespace VenueOps.Models;

public sealed class LoginResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;

    [JsonPropertyName("user")]
    public UserInfo? User { get; set; }
}
