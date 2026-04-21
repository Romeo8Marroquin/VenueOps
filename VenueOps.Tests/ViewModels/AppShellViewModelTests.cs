using Moq;
using VenueOps.Models;
using VenueOps.Services;
using VenueOps.ViewModels;

namespace VenueOps.Tests.ViewModels;

public class AppShellViewModelTests
{
    private readonly Mock<INavigationService> _navMock = new();
    private readonly Mock<ISessionService> _sessionMock = new();

    private AppShellViewModel CreateSut() => new(_navMock.Object, _sessionMock.Object);

    // ── UserName ─────────────────────────────────────────────────────────────

    [Fact]
    public void UserName_ReturnsUserName_WhenCurrentUserIsSet()
    {
        _sessionMock.Setup(s => s.CurrentUser).Returns(new UserInfo { Name = "Alice Smith" });

        Assert.Equal("Alice Smith", CreateSut().UserName);
    }

    [Fact]
    public void UserName_ReturnsFallback_WhenCurrentUserIsNull()
    {
        _sessionMock.Setup(s => s.CurrentUser).Returns((UserInfo?)null);

        Assert.Equal("User", CreateSut().UserName);
    }

    // ── ShortEmail ────────────────────────────────────────────────────────────

    [Fact]
    public void ShortEmail_ReturnsLocalPart_WhenEmailContainsAt()
    {
        _sessionMock.Setup(s => s.CurrentUser).Returns(new UserInfo { Email = "demo@example.com" });

        Assert.Equal("demo", CreateSut().ShortEmail);
    }

    [Fact]
    public void ShortEmail_ReturnsFullString_WhenEmailHasNoAt()
    {
        _sessionMock.Setup(s => s.CurrentUser).Returns(new UserInfo { Email = "noemail" });

        Assert.Equal("noemail", CreateSut().ShortEmail);
    }

    [Fact]
    public void ShortEmail_ReturnsEmpty_WhenCurrentUserIsNull()
    {
        _sessionMock.Setup(s => s.CurrentUser).Returns((UserInfo?)null);

        Assert.Equal(string.Empty, CreateSut().ShortEmail);
    }

    // ── UserInitial ───────────────────────────────────────────────────────────

    [Fact]
    public void UserInitial_ReturnsFirstLetterUppercased()
    {
        _sessionMock.Setup(s => s.CurrentUser).Returns(new UserInfo { Name = "demo user" });

        Assert.Equal("D", CreateSut().UserInitial);
    }

    [Fact]
    public void UserInitial_ReturnsFallback_WhenCurrentUserIsNull()
    {
        _sessionMock.Setup(s => s.CurrentUser).Returns((UserInfo?)null);

        Assert.Equal("U", CreateSut().UserInitial);
    }

    // ── SignOutCommand ────────────────────────────────────────────────────────

    [Fact]
    public async Task SignOutCommand_ClearsUserAndNavigatesToLogin()
    {
        _sessionMock.SetupProperty(s => s.CurrentUser, new UserInfo { Name = "Alice" });
        _navMock.Setup(n => n.NavigateToLoginAsync()).Returns(Task.CompletedTask);
        AppShellViewModel sut = CreateSut();

        await sut.SignOutCommand.ExecuteAsync(null);

        _sessionMock.VerifySet(s => s.CurrentUser = null, Times.Once);
        _navMock.Verify(n => n.NavigateToLoginAsync(), Times.Once);
    }

    [Fact]
    public async Task SignOutCommand_DoesNotNavigate_WhenAlreadyBusy()
    {
        AppShellViewModel sut = CreateSut();
        sut.IsBusy = true;

        await sut.SignOutCommand.ExecuteAsync(null);

        _navMock.Verify(n => n.NavigateToLoginAsync(), Times.Never);
    }
}
