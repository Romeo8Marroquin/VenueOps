using VenueOps.Models;
using VenueOps.Services;

namespace VenueOps.Tests.Services;

public class SessionServiceTests
{
    [Fact]
    public void CurrentUser_IsNull_ByDefault()
    {
        Assert.Null(new SessionService().CurrentUser);
    }

    [Fact]
    public void CurrentUser_StoresValueAndRaisesPropertyChanged_WhenSet()
    {
        SessionService sut = new();
        UserInfo user = new() { Uuid = "fake-uuid-100", Name = "Ivy Quill", Email = "ivy@example.invalid" };
        List<string?> raised = [];
        sut.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        sut.CurrentUser = user;

        Assert.Same(user, sut.CurrentUser);
        Assert.Equal([nameof(ISessionService.CurrentUser)], raised);
    }

    [Fact]
    public void CurrentUser_DoesNotRaisePropertyChanged_WhenSetToSameInstance()
    {
        UserInfo user = new() { Name = "Ivy Quill" };
        SessionService sut = new() { CurrentUser = user };
        int raised = 0;
        sut.PropertyChanged += (_, _) => raised++;

        sut.CurrentUser = user;

        Assert.Equal(0, raised);
    }
}
