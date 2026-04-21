using System.Net;
using Moq;
using VenueOps.Models;
using VenueOps.Services;
using VenueOps.ViewModels;

namespace VenueOps.Tests.ViewModels;

public class LoginViewModelTests
{
    private readonly Mock<IAuthService> _authMock = new();
    private readonly Mock<INavigationService> _navMock = new();
    private readonly Mock<ISessionService> _sessionMock = new();

    private LoginViewModel CreateSut() => new(_authMock.Object, _navMock.Object, _sessionMock.Object);

    // ── Validation ────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("", "anypassword")]
    [InlineData("   ", "anypassword")]
    [InlineData("user@fake.dev", "")]
    [InlineData("user@fake.dev", "   ")]
    public async Task LoginCommand_SetsValidationError_WhenEmailOrPasswordIsEmpty(string email, string password)
    {
        LoginViewModel sut = CreateSut();
        sut.Email    = email;
        sut.Password = password;

        await sut.LoginCommand.ExecuteAsync(null);

        Assert.Equal("Please enter your email and password.", sut.ErrorMessage);
        Assert.True(sut.HasError);
        _authMock.Verify(a => a.LoginAsync(It.IsAny<LoginRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Success path ──────────────────────────────────────────────────────────

    [Fact]
    public async Task LoginCommand_StoresUserClearsPasswordAndNavigates_OnSuccess()
    {
        UserInfo fakeUser = new() { Uuid = "fake-uuid-001", Name = "Test User", Email = "test@fake.dev" };
        LoginResponse fakeResponse = new() { Success = true, Token = "fake-token-xyz", User = fakeUser };

        _authMock
            .Setup(a => a.LoginAsync(It.IsAny<LoginRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(fakeResponse);
        _navMock.Setup(n => n.NavigateToMainAsync()).Returns(Task.CompletedTask);

        LoginViewModel sut = CreateSut();
        sut.Email    = "test@fake.dev";
        sut.Password = "S3cr3tPass!";

        await sut.LoginCommand.ExecuteAsync(null);

        _sessionMock.VerifySet(s => s.CurrentUser = fakeUser, Times.Once);
        Assert.Equal(string.Empty, sut.Password);
        _navMock.Verify(n => n.NavigateToMainAsync(), Times.Once);
        Assert.Equal(string.Empty, sut.ErrorMessage);
        Assert.False(sut.IsBusy);
    }

    // ── AuthException error mapping ───────────────────────────────────────────

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized,        "Invalid email or password.")]
    [InlineData(HttpStatusCode.Forbidden,           "Access denied.")]
    [InlineData(HttpStatusCode.InternalServerError, "Server error. Please try again later.")]
    [InlineData(HttpStatusCode.ServiceUnavailable,  "Server error. Please try again later.")]
    [InlineData(HttpStatusCode.BadRequest,          "Sign-in failed. Please try again.")]
    public async Task LoginCommand_MapsAuthExceptionStatusToMessage(HttpStatusCode code, string expectedMessage)
    {
        _authMock
            .Setup(a => a.LoginAsync(It.IsAny<LoginRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AuthException(code));

        LoginViewModel sut = CreateSut();
        sut.Email    = "user@fake.dev";
        sut.Password = "AnyPass1!";

        await sut.LoginCommand.ExecuteAsync(null);

        Assert.Equal(expectedMessage, sut.ErrorMessage);
        Assert.True(sut.HasError);
        Assert.False(sut.IsBusy);
    }

    [Fact]
    public async Task LoginCommand_SetsGenericAuthError_WhenStatusCodeIsNull()
    {
        _authMock
            .Setup(a => a.LoginAsync(It.IsAny<LoginRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AuthException(null));

        LoginViewModel sut = CreateSut();
        sut.Email    = "user@fake.dev";
        sut.Password = "AnyPass1!";

        await sut.LoginCommand.ExecuteAsync(null);

        Assert.Equal("Sign-in failed. Please try again.", sut.ErrorMessage);
    }

    // ── Network / unexpected errors ───────────────────────────────────────────

    [Fact]
    public async Task LoginCommand_SetsConnectionError_OnHttpRequestException()
    {
        _authMock
            .Setup(a => a.LoginAsync(It.IsAny<LoginRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("network down"));

        LoginViewModel sut = CreateSut();
        sut.Email    = "user@fake.dev";
        sut.Password = "AnyPass1!";

        await sut.LoginCommand.ExecuteAsync(null);

        Assert.Equal("Unable to connect. Please check your connection.", sut.ErrorMessage);
        Assert.False(sut.IsBusy);
    }

    [Fact]
    public async Task LoginCommand_SetsUnexpectedError_OnGenericException()
    {
        _authMock
            .Setup(a => a.LoginAsync(It.IsAny<LoginRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("unexpected"));

        LoginViewModel sut = CreateSut();
        sut.Email    = "user@fake.dev";
        sut.Password = "AnyPass1!";

        await sut.LoginCommand.ExecuteAsync(null);

        Assert.Equal("An unexpected error occurred. Please try again.", sut.ErrorMessage);
        Assert.False(sut.IsBusy);
    }
}
