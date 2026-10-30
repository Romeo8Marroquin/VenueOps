using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace VenueOps.Tests.Views;

/// <summary>
/// Reads EventDetailView.xaml and checks the tags card's heading binding in both layouts.
/// The view is outside unit-test scope, but the binding is what makes TagsHeadingText reach
/// the screen, so it is checked here against the XAML source.
/// </summary>
public class EventDetailViewBindingTests
{
    private const string TagsHeadingBinding = "{Binding TagsHeadingText}";
    private const string HasTagsBinding = "{Binding HasTags}";
    private const string CompactLayoutBinding = "{Binding IsCompactLayout}";
    private const string WideLayoutBinding = "{Binding IsWideLayout}";
    private const string CardSectionLabelStyle = "{StaticResource CardSectionLabel}";

    private static readonly XNamespace Maui = "http://schemas.microsoft.com/dotnet/2021/maui";

    private static XDocument LoadView([CallerFilePath] string testFile = "")
    {
        // This file lives in VenueOps.Tests/Views, so the repository root is two levels up.
        string testDirectory = Path.GetDirectoryName(testFile)!;
        string path = Path.GetFullPath(
            Path.Combine(testDirectory, "..", "..", "VenueOps", "Views", "Events", "EventDetailView.xaml"));

        Assert.True(File.Exists(path), $"EventDetailView.xaml not found at {path}");
        return XDocument.Load(path);
    }

    // The tags cards are the Borders shown by HasTags: one in the compact layout, one in the wide layout.
    private static List<XElement> TagsCards(XDocument view) =>
        view.Descendants(Maui + "Border")
            .Where(border => (string?)border.Attribute("IsVisible") == HasTagsBinding)
            .ToList();

    // The nearest ancestor that is shown by a layout binding names the layout the card belongs to.
    private static string? LayoutOf(XElement card) =>
        card.Ancestors()
            .Select(ancestor => (string?)ancestor.Attribute("IsVisible"))
            .FirstOrDefault(binding => binding is CompactLayoutBinding or WideLayoutBinding);

    // The card's section heading is the one Label styled as a card section label.
    private static XElement HeadingLabelOf(XElement card) =>
        Assert.Single(card.Descendants(Maui + "Label")
            .Where(label => (string?)label.Attribute("Style") == CardSectionLabelStyle));

    [Fact]
    public void TagsCard_IsPresentInBothLayouts()
    {
        XDocument view = LoadView();

        List<XElement> cards = TagsCards(view);

        Assert.Equal(2, cards.Count);
        string?[] layouts = cards.Select(LayoutOf).ToArray();
        Assert.Contains(CompactLayoutBinding, layouts);
        Assert.Contains(WideLayoutBinding, layouts);
    }

    [Fact]
    public void TagsCard_HeadingTextIsBoundToTagsHeadingText_InBothLayouts()
    {
        XDocument view = LoadView();

        foreach (XElement card in TagsCards(view))
        {
            XElement heading = HeadingLabelOf(card);

            Assert.Equal(TagsHeadingBinding, (string?)heading.Attribute("Text"));
        }
    }

    [Fact]
    public void TagsCard_DoesNotUseALiteralTagsHeading_InEitherLayout()
    {
        XDocument view = LoadView();

        foreach (XElement card in TagsCards(view))
        {
            foreach (XElement label in card.Descendants(Maui + "Label"))
            {
                string text = (string?)label.Attribute("Text") ?? string.Empty;

                Assert.DoesNotContain("TAGS", text);
            }
        }
    }
}
