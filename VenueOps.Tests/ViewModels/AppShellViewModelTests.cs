using System.ComponentModel;
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

    [Fact]
    public async Task SignOutCommand_ResetsBusy_AfterNavigating()
    {
        _navMock.Setup(n => n.NavigateToLoginAsync()).Returns(Task.CompletedTask);
        AppShellViewModel sut = CreateSut();

        await sut.SignOutCommand.ExecuteAsync(null);

        Assert.False(sut.IsBusy);
    }

    [Fact]
    public void IsBusy_RaisesSignOutCommandCanExecuteChanged()
    {
        AppShellViewModel sut = CreateSut();
        int raised = 0;
        sut.SignOutCommand.CanExecuteChanged += (_, _) => raised++;

        sut.IsBusy = true;
        sut.IsBusy = false;

        Assert.Equal(2, raised);
    }

    // ── Additional edge cases ─────────────────────────────────────────────────

    [Fact]
    public void ShortEmail_ReturnsFullString_WhenEmailStartsWithAt()
    {
        _sessionMock.Setup(s => s.CurrentUser).Returns(new UserInfo { Email = "@example.invalid" });

        Assert.Equal("@example.invalid", CreateSut().ShortEmail);
    }

    [Fact]
    public void UserInitial_ReturnsFallback_WhenNameIsEmpty()
    {
        _sessionMock.Setup(s => s.CurrentUser).Returns(new UserInfo { Name = string.Empty });

        Assert.Equal("U", CreateSut().UserInitial);
    }

    // ── Session change notifications ──────────────────────────────────────────

    [Fact]
    public void SessionCurrentUserChange_RaisesUserDisplayNotifications()
    {
        AppShellViewModel sut = CreateSut();
        List<string?> raised = [];
        sut.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        _sessionMock.Raise(s => s.PropertyChanged += null, new PropertyChangedEventArgs(nameof(ISessionService.CurrentUser)));

        Assert.Equal(
            [nameof(AppShellViewModel.UserName), nameof(AppShellViewModel.ShortEmail), nameof(AppShellViewModel.UserInitial)],
            raised);
    }

    [Fact]
    public void SessionOtherPropertyChange_RaisesNothing()
    {
        AppShellViewModel sut = CreateSut();
        List<string?> raised = [];
        sut.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        _sessionMock.Raise(s => s.PropertyChanged += null, new PropertyChangedEventArgs("SomethingElse"));

        Assert.Empty(raised);
    }

    [Fact]
    public void RealSessionService_UserChangeUpdatesDisplayedValues()
    {
        SessionService session = new();
        AppShellViewModel sut = new(_navMock.Object, session);
        List<string?> raised = [];
        sut.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        session.CurrentUser = new UserInfo { Name = "bea stone", Email = "bea@example.invalid" };

        Assert.Contains(nameof(AppShellViewModel.UserName), raised);
        Assert.Equal("bea stone", sut.UserName);
        Assert.Equal("bea", sut.ShortEmail);
        Assert.Equal("B", sut.UserInitial);
    }
}
