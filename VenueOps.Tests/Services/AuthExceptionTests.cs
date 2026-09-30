using System.Net;
using VenueOps.Services;

namespace VenueOps.Tests.Services;

public class AuthExceptionTests
{
    [Fact]
    public void Constructor_IncludesNumericStatusInMessage_WhenStatusCodeIsProvided()
    {
        AuthException ex = new(HttpStatusCode.Unauthorized);

        Assert.Equal(HttpStatusCode.Unauthorized, ex.StatusCode);
        Assert.Equal("Authentication failed (401).", ex.Message);
    }

    [Fact]
    public void Constructor_OmitsStatusFromMessage_WhenStatusCodeIsNull()
    {
        AuthException ex = new();

        Assert.Null(ex.StatusCode);
        Assert.Equal("Authentication failed.", ex.Message);
    }
}
