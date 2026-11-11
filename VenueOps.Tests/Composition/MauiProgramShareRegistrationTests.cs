using VenueOps.Tests.Support;

namespace VenueOps.Tests.Composition;

public class MauiProgramShareRegistrationTests
{
    // MauiProgram is not compiled for the plain net10.0 test target, so its registrations are read as text.
    private static readonly string Source = RepositoryFile.ReadAllText("VenueOps/MauiProgram.cs");

    [Fact]
    public void IShareService_IsRegisteredWithShareService()
    {
        Assert.Matches(@"(?m)^\s*builder\.Services\.AddSingleton<IShareService,\s*ShareService>\(\);", Source);
    }

    [Fact]
    public void EventDetailViewModel_AndItsDependencies_AreRegistered()
    {
        Assert.Matches(@"(?m)^\s*builder\.Services\.AddTransient<EventDetailViewModel>\(\);", Source);
        Assert.Matches(@"(?m)^\s*builder\.Services\.AddSingleton<IEventsService,\s*EventsService>\(\);", Source);
        Assert.Matches(@"(?m)^\s*builder\.Services\.AddSingleton<INavigationService,\s*NavigationService>\(\);", Source);
    }
}
