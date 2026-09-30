using System.Net;
using Moq;
using VenueOps.Models;
using VenueOps.Services;
using VenueOps.ViewModels;

namespace VenueOps.Tests.ViewModels;

public class RegisterViewModelTests
{
    private readonly Mock<IAuthService> _authMock = new();
    private readonly Mock<INavigationService> _navMock = new();
    private readonly Mock<IDialogService> _dialogMock = new();

    public static TheoryData<string, string, string, string> MissingFieldCases =
        new()
        {
            { string.Empty, CreateEmail(), CreateOpaqueValue(), CreateOpaqueValue() },
            { "   ", CreateEmail(), CreateOpaqueValue(), CreateOpaqueValue() },
            { CreateName(), string.Empty, CreateOpaqueValue(), CreateOpaqueValue() },
            { CreateName(), CreateEmail(), "   ", CreateOpaqueValue() },
            { CreateName(), CreateEmail(), CreateOpaqueValue(), string.Empty }
        };

    public RegisterViewModelTests()
    {
        _dialogMock
            .Setup(d => d.ShowAlertAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        _navMock.Setup(n => n.NavigateToLoginAsync()).Returns(Task.CompletedTask);
    }

    private RegisterViewModel CreateSut() => new(_authMock.Object, _navMock.Object, _dialogMock.Object);

    private static string CreateName() => "Test Person";

    private static string CreateEmail() => "person@example.test";

    private static string CreateOpaqueValue() => new('x', 12);

    private RegisterViewModel CreateFilledSut()
    {
        RegisterViewModel sut = CreateSut();
        sut.Name = CreateName();
        sut.Email = CreateEmail();
        sut.Password = CreateOpaqueValue();
        sut.PasswordConfirmation = CreateOpaqueValue();
        return sut;
    }

    private void SetupRegisterThrows(Exception ex) =>
        _authMock
            .Setup(a => a.RegisterAsync(It.IsAny<RegisterRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(ex);

    // ── Initial state ─────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_SetsTitleAndEmptyFields()
    {
        RegisterViewModel sut = CreateSut();

        Assert.Equal("Create Account", sut.Title);
        Assert.Equal(string.Empty, sut.Name);
        Assert.Equal(string.Empty, sut.Email);
        Assert.Equal(string.Empty, sut.Password);
        Assert.Equal(string.Empty, sut.PasswordConfirmation);
        Assert.False(sut.HasError);
    }

    [Fact]
    public void ErrorMessage_RaisesHasErrorNotification()
    {
        RegisterViewModel sut = CreateSut();
        List<string?> raised = [];
        sut.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        sut.ErrorMessage = "Some error";

        Assert.True(sut.HasError);
        Assert.Contains(nameof(RegisterViewModel.HasError), raised);
    }

    // ── Validation ────────────────────────────────────────────────────────────

    [Theory]
    [MemberData(nameof(MissingFieldCases))]
    public async Task RegisterCommand_SetsValidationError_WhenAnyFieldIsBlank(
        string name, string email, string password, string confirmation)
    {
        RegisterViewModel sut = CreateSut();
        sut.Name = name;
        sut.Email = email;
        sut.Password = password;
        sut.PasswordConfirmation = confirmation;

        await sut.RegisterCommand.ExecuteAsync(null);

        Assert.Equal("Please fill in all fields.", sut.ErrorMessage);
        Assert.True(sut.HasError);
        _authMock.Verify(a => a.RegisterAsync(It.IsAny<RegisterRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RegisterCommand_SetsMismatchError_WhenPasswordsDiffer()
    {
        RegisterViewModel sut = CreateFilledSut();
        sut.PasswordConfirmation = new string('z', 12);

        await sut.RegisterCommand.ExecuteAsync(null);

        Assert.Equal("Passwords do not match.", sut.ErrorMessage);
        _authMock.Verify(a => a.RegisterAsync(It.IsAny<RegisterRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RegisterCommand_DoesNothing_WhenAlreadyBusy()
    {
        RegisterViewModel sut = CreateFilledSut();
        sut.IsBusy = true;

        await sut.RegisterCommand.ExecuteAsync(null);

        Assert.Equal(string.Empty, sut.ErrorMessage);
        _authMock.Verify(a => a.RegisterAsync(It.IsAny<RegisterRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        _dialogMock.Verify(d => d.ShowAlertAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    // ── Success path ──────────────────────────────────────────────────────────

    [Fact]
    public async Task RegisterCommand_SendsRequestShowsWelcomeAndNavigatesToLogin_OnSuccess()
    {
        RegisterRequest? sent = null;
        bool busyWhenDialogShown = true;
        _authMock
            .Setup(a => a.RegisterAsync(It.IsAny<RegisterRequest>(), It.IsAny<CancellationToken>()))
            .Callback<RegisterRequest, CancellationToken>((r, _) => sent = r)
            .ReturnsAsync(new RegisterResponse { Success = true, User = new UserInfo { Name = "Test Person" } });
        RegisterViewModel sut = CreateFilledSut();
        _dialogMock
            .Setup(d => d.ShowAlertAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Callback(() => busyWhenDialogShown = sut.IsBusy)
            .Returns(Task.CompletedTask);
        sut.ErrorMessage = "stale error";

        await sut.RegisterCommand.ExecuteAsync(null);

        Assert.NotNull(sent);
        Assert.Equal(CreateName(), sent.Name);
        Assert.Equal(CreateEmail(), sent.Email);
        Assert.Equal(CreateOpaqueValue(), sent.Password);
        Assert.Equal(CreateOpaqueValue(), sent.PasswordConfirmation);
        Assert.Equal(string.Empty, sut.Password);
        Assert.Equal(string.Empty, sut.PasswordConfirmation);
        Assert.Equal(string.Empty, sut.ErrorMessage);
        Assert.False(busyWhenDialogShown);
        _dialogMock.Verify(d => d.ShowAlertAsync("Account created", "Welcome, Test Person! You can now sign in.", "Sign in"), Times.Once);
        _navMock.Verify(n => n.NavigateToLoginAsync(), Times.Once);
        Assert.False(sut.IsBusy);
    }

    // ── AuthException error mapping ───────────────────────────────────────────

    [Theory]
    [InlineData(HttpStatusCode.Conflict, "An account with this email already exists.")]
    [InlineData(HttpStatusCode.InternalServerError, "Server error. Please try again later.")]
    [InlineData(HttpStatusCode.ServiceUnavailable, "Server error. Please try again later.")]
    [InlineData(HttpStatusCode.BadRequest, "Registration failed. Please try again.")]
    public async Task RegisterCommand_ShowsMappedDialog_OnAuthException(HttpStatusCode code, string expectedMessage)
    {
        SetupRegisterThrows(new AuthException(code));
        RegisterViewModel sut = CreateFilledSut();

        await sut.RegisterCommand.ExecuteAsync(null);

        _dialogMock.Verify(d => d.ShowAlertAsync("Registration failed", expectedMessage, "OK"), Times.Once);
        _navMock.Verify(n => n.NavigateToLoginAsync(), Times.Never);
        Assert.False(sut.IsBusy);
    }

    [Fact]
    public async Task RegisterCommand_ShowsGenericFailureDialog_WhenAuthStatusIsNull()
    {
        SetupRegisterThrows(new AuthException(null));
        RegisterViewModel sut = CreateFilledSut();

        await sut.RegisterCommand.ExecuteAsync(null);

        _dialogMock.Verify(d => d.ShowAlertAsync("Registration failed", "Registration failed. Please try again.", "OK"), Times.Once);
    }

    // ── Network / unexpected errors ───────────────────────────────────────────

    [Fact]
    public async Task RegisterCommand_ShowsConnectionDialog_OnHttpRequestException()
    {
        SetupRegisterThrows(new HttpRequestException("network down"));
        RegisterViewModel sut = CreateFilledSut();

        await sut.RegisterCommand.ExecuteAsync(null);

        _dialogMock.Verify(
            d => d.ShowAlertAsync("Connection error", "Unable to connect. Please check your connection and try again.", "OK"),
            Times.Once);
        _navMock.Verify(n => n.NavigateToLoginAsync(), Times.Never);
        Assert.False(sut.IsBusy);
    }

    [Fact]
    public async Task RegisterCommand_ShowsUnexpectedDialog_OnGenericException()
    {
        SetupRegisterThrows(new InvalidOperationException("unexpected"));
        RegisterViewModel sut = CreateFilledSut();

        await sut.RegisterCommand.ExecuteAsync(null);

        _dialogMock.Verify(d => d.ShowAlertAsync("Unexpected error", "Something went wrong. Please try again.", "OK"), Times.Once);
        _navMock.Verify(n => n.NavigateToLoginAsync(), Times.Never);
        Assert.False(sut.IsBusy);
    }

    [Fact]
    public async Task RegisterCommand_ShowsUnexpectedDialog_WhenSuccessResponseHasNoUser()
    {
        _authMock
            .Setup(a => a.RegisterAsync(It.IsAny<RegisterRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RegisterResponse { Success = true, User = null });
        RegisterViewModel sut = CreateFilledSut();

        await sut.RegisterCommand.ExecuteAsync(null);

        _dialogMock.Verify(d => d.ShowAlertAsync("Unexpected error", "Something went wrong. Please try again.", "OK"), Times.Once);
        _navMock.Verify(n => n.NavigateToLoginAsync(), Times.Never);
    }

    // ── Busy state / navigation ───────────────────────────────────────────────

    [Fact]
    public void IsBusy_RaisesRegisterCommandCanExecuteChanged()
    {
        RegisterViewModel sut = CreateSut();
        int raised = 0;
        sut.RegisterCommand.CanExecuteChanged += (_, _) => raised++;

        sut.IsBusy = true;
        sut.IsBusy = false;

        Assert.Equal(2, raised);
    }

    [Fact]
    public async Task NavigateToLoginCommand_NavigatesToLogin()
    {
        await CreateSut().NavigateToLoginCommand.ExecuteAsync(null);

        _navMock.Verify(n => n.NavigateToLoginAsync(), Times.Once);
    }
}
