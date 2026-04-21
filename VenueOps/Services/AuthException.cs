using System.Net;

namespace VenueOps.Services;

public sealed class AuthException(HttpStatusCode? statusCode = null)
    : Exception($"Authentication failed{(statusCode.HasValue ? $" ({(int)statusCode})" : string.Empty)}.")
{
    public HttpStatusCode? StatusCode { get; } = statusCode;
}
