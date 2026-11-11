using System.Reflection;
using VenueOps.Services;
using VenueOps.Tests.Support;
using VenueOps.ViewModels;

namespace VenueOps.Tests.ViewModels;

public class EventDetailViewModelShareWiringTests
{
    [Fact]
    public void ViewModelSource_NeverCallsShareDefault()
    {
        string source = RepositoryFile.ReadAllText("VenueOps/ViewModels/EventDetailViewModel.cs");

        Assert.DoesNotContain("Share.Default", source);
    }

    [Fact]
    public void Constructor_TakesIShareService()
    {
        ConstructorInfo[] constructors = typeof(EventDetailViewModel).GetConstructors();

        ConstructorInfo constructor = Assert.Single(constructors);
        Assert.Contains(typeof(IShareService), constructor.GetParameters().Select(p => p.ParameterType));
    }
}
