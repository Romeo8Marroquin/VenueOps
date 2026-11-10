using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace VenueOps.Tests.Views;

/// <summary>
/// Structural check of the event detail XAML: the Share button, its semantic description, its
/// command binding, and that nothing hides it. Reads the view file from the repository, found
/// relative to this test source file.
/// </summary>
public class EventDetailViewXamlTests
{
    private static readonly XNamespace Maui = "http://schemas.microsoft.com/dotnet/2021/maui";

    private static XDocument LoadEventDetailView([CallerFilePath] string testSourcePath = "")
    {
        string testsFolder = Path.GetDirectoryName(testSourcePath)!;           // VenueOps.Tests/Views
        string repoRoot = Path.GetFullPath(Path.Combine(testsFolder, "..", "..")); // repository root
        return XDocument.Load(Path.Combine(repoRoot, "VenueOps", "Views", "Events", "EventDetailView.xaml"));
    }

    [Fact]
    public void EventDetailView_DeclaresShareButtonWithSemanticDescriptionAndCommand_AndNothingHidesIt()
    {
        XDocument xaml = LoadEventDetailView();

        XElement button = Assert.Single(xaml.Descendants(Maui + "Button")
            .Where(b => (string?)b.Attribute("Text") == "Share"));

        Assert.Equal("Share event", (string?)button.Attribute("SemanticProperties.Description"));
        Assert.Equal("{Binding ShareCommand}", (string?)button.Attribute("Command"));
        for (XElement? element = button; element is not null; element = element.Parent)
            Assert.Null(element.Attribute("IsVisible"));
    }
}
