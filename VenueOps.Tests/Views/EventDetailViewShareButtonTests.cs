using System.Xml.Linq;
using VenueOps.Tests.Support;

namespace VenueOps.Tests.Views;

public class EventDetailViewShareButtonTests
{
    private const string ShareBinding = "{Binding ShareCommand}";

    private static XDocument LoadView() =>
        XDocument.Parse(RepositoryFile.ReadAllText("VenueOps/Views/Events/EventDetailView.xaml"));

    private static XElement FindShareButton(XDocument view) =>
        view.Descendants().Single(e => e.Name.LocalName == "Button" && e.Attribute("Command")?.Value == ShareBinding);

    [Fact]
    public void ShareButton_IsTextOnly_BoundToShareCommand_WithSemanticDescription()
    {
        XElement button = FindShareButton(LoadView());

        Assert.Equal("Share", button.Attribute("Text")?.Value);
        Assert.Equal(ShareBinding, button.Attribute("Command")?.Value);
        Assert.Equal("Share event", button.Attribute("SemanticProperties.Description")?.Value);
        Assert.Null(button.Attribute("ImageSource"));
        Assert.Null(button.Attribute("Source"));
        Assert.DoesNotContain(button.Elements(), e => e.Name.LocalName == "Button.ImageSource");
    }

    [Fact]
    public void ShareButton_AndItsAncestors_CarryNoVisibilityBinding()
    {
        XElement button = FindShareButton(LoadView());

        Assert.Null(button.Attribute("IsVisible"));
        foreach (XElement ancestor in button.Ancestors())
            Assert.Null(ancestor.Attribute("IsVisible"));
    }

    [Fact]
    public void Screen_HasNoShareResultElement_OtherThanTheShareButton()
    {
        XDocument view = LoadView();

        XElement[] shareTexts = view.Descendants()
            .Where(e => e.Attributes("Text").Any(a => a.Value.Contains("share", StringComparison.OrdinalIgnoreCase)))
            .ToArray();
        Assert.Equal(new[] { "Button" }, shareTexts.Select(e => e.Name.LocalName));

        Assert.DoesNotContain(view.Descendants(), e =>
            e.Name.LocalName.Contains("Toast", StringComparison.Ordinal) ||
            e.Name.LocalName.Contains("Snackbar", StringComparison.Ordinal) ||
            e.Name.LocalName.Contains("Popup", StringComparison.Ordinal));
    }
}
