using System.Net;
using System.Text.Json;
using Moq;
using VenueOps.Models;
using VenueOps.Services;
using VenueOps.Tests.TestDoubles;

namespace VenueOps.Tests.Services;

public class AuthServiceTests
{
    private const string LoginSuccessJson =
        """{"success":true,"token":"fake-token-abc","user":{"uuid":"fake-uuid-001","name":"Nora Vale","email":"nora@example.invalid","role":"organizer"}}""";

    private const string RegisterSuccessJson =
        """{"success":true,"user":{"uuid":"fake-uuid-002","name":"Omar Reed","email":"omar@example.invalid","role":"organizer"}}""";

    private static LoginRequest CreateLoginRequest() => new() { Email = "nora@example.invalid", Password = new string('x', 12) };

    private static RegisterRequest CreateRegisterRequest() => new()
    {
        Name = "Omar Reed",
        Email = "omar@example.invalid",
        Password = new string('y', 12),
        PasswordConfirmation = new string('y', 12)
    };

    private static AuthService CreateSut(StubHttpMessageHandler handler) => new(FakeApi.CreateFactory(handler).Object);

    // ── Construction ──────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_CreatesNamedApiClient()
    {
        Mock<IHttpClientFactory> factory = FakeApi.CreateFactory(FakeApi.Respond(HttpStatusCode.OK));

        _ = new AuthService(factory.Object);

        factory.Verify(f => f.CreateClient(FakeApi.ClientName), Times.Once);
    }

    // ── LoginAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task LoginAsync_PostsCredentialsToSignInEndpoint()
    {
        StubHttpMessageHandler handler = FakeApi.Respond(HttpStatusCode.OK, LoginSuccessJson);
        LoginRequest request = CreateLoginRequest();

        await CreateSut(handler).LoginAsync(request);

        Assert.Equal(HttpMethod.Post, handler.LastRequest.Method);
        Assert.Equal("auth/sign-in", handler.LastRequest.RelativeUrl);
        using JsonDocument body = JsonDocument.Parse(handler.LastRequest.Body!);
        Assert.Equal(request.Email, body.RootElement.GetProperty("email").GetString());
        Assert.Equal(request.Password, body.RootElement.GetProperty("password").GetString());
    }

    [Fact]
    public async Task LoginAsync_ReturnsDeserializedResponse_OnSuccess()
    {
        LoginResponse result = await CreateSut(FakeApi.Respond(HttpStatusCode.OK, LoginSuccessJson))
            .LoginAsync(CreateLoginRequest());

        Assert.True(result.Success);
        Assert.Equal("fake-token-abc", result.Token);
        Assert.NotNull(result.User);
        Assert.Equal("fake-uuid-001", result.User.Uuid);
        Assert.Equal("Nora Vale", result.User.Name);
        Assert.Equal("nora@example.invalid", result.User.Email);
        Assert.Equal("organizer", result.User.Role);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task LoginAsync_ThrowsAuthExceptionWithStatus_OnNonSuccessStatus(HttpStatusCode code)
    {
        AuthService sut = CreateSut(FakeApi.Respond(code));

        AuthException ex = await Assert.ThrowsAsync<AuthException>(() => sut.LoginAsync(CreateLoginRequest()));

        Assert.Equal(code, ex.StatusCode);
    }

    [Theory]
    [InlineData("""{"success":false,"token":"","user":null}""")]
    [InlineData("null")]
    public async Task LoginAsync_ThrowsAuthExceptionWithoutStatus_WhenBodyIsNotASuccess(string json)
    {
        AuthService sut = CreateSut(FakeApi.Respond(HttpStatusCode.OK, json));

        AuthException ex = await Assert.ThrowsAsync<AuthException>(() => sut.LoginAsync(CreateLoginRequest()));

        Assert.Null(ex.StatusCode);
    }

    // ── RegisterAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task RegisterAsync_PostsAllFieldsToSignUpEndpoint()
    {
        StubHttpMessageHandler handler = FakeApi.Respond(HttpStatusCode.OK, RegisterSuccessJson);
        RegisterRequest request = CreateRegisterRequest();

        await CreateSut(handler).RegisterAsync(request);

        Assert.Equal(HttpMethod.Post, handler.LastRequest.Method);
        Assert.Equal("auth/sign-up", handler.LastRequest.RelativeUrl);
        using JsonDocument body = JsonDocument.Parse(handler.LastRequest.Body!);
        Assert.Equal(request.Name, body.RootElement.GetProperty("name").GetString());
        Assert.Equal(request.Email, body.RootElement.GetProperty("email").GetString());
        Assert.Equal(request.Password, body.RootElement.GetProperty("password").GetString());
        Assert.Equal(request.PasswordConfirmation, body.RootElement.GetProperty("passwordConfirmation").GetString());
    }

    [Fact]
    public async Task RegisterAsync_ReturnsDeserializedResponse_OnSuccess()
    {
        RegisterResponse result = await CreateSut(FakeApi.Respond(HttpStatusCode.OK, RegisterSuccessJson))
            .RegisterAsync(CreateRegisterRequest());

        Assert.True(result.Success);
        Assert.NotNull(result.User);
        Assert.Equal("Omar Reed", result.User.Name);
    }

    [Theory]
    [InlineData(HttpStatusCode.Conflict)]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task RegisterAsync_ThrowsAuthExceptionWithStatus_OnNonSuccessStatus(HttpStatusCode code)
    {
        AuthService sut = CreateSut(FakeApi.Respond(code));

        AuthException ex = await Assert.ThrowsAsync<AuthException>(() => sut.RegisterAsync(CreateRegisterRequest()));

        Assert.Equal(code, ex.StatusCode);
    }

    [Theory]
    [InlineData("""{"success":false,"user":null}""")]
    [InlineData("null")]
    public async Task RegisterAsync_ThrowsAuthExceptionWithoutStatus_WhenBodyIsNotASuccess(string json)
    {
        AuthService sut = CreateSut(FakeApi.Respond(HttpStatusCode.OK, json));

        AuthException ex = await Assert.ThrowsAsync<AuthException>(() => sut.RegisterAsync(CreateRegisterRequest()));

        Assert.Null(ex.StatusCode);
    }
}
