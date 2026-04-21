using System.Text.Json.Serialization;

namespace VenueOps.Models;

public sealed class RegisterRequest
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;

    [JsonPropertyName("passwordConfirmation")]
    public string PasswordConfirmation { get; set; } = string.Empty;
}
