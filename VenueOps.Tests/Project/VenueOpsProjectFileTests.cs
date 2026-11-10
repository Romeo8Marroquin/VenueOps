using System.Xml.Linq;

namespace VenueOps.Tests.Project;

public class VenueOpsProjectFileTests
{
    private const string NewtonsoftJson = "Newtonsoft.Json";

    private static readonly string[] _otherPackagesInItemGroup =
    [
        "CommunityToolkit.Maui", "CommunityToolkit.Mvvm", "Microsoft.Extensions.Http",
        "Microsoft.Maui.Controls", "Microsoft.Extensions.Logging.Debug", "SonarAnalyzer.CSharp"
    ];

    private static string LoadAppProjectText()
    {
        string? dir = AppContext.BaseDirectory;
        while (dir is not null && !File.Exists(Path.Combine(dir, "VenueOps", "VenueOps.csproj")))
            dir = Path.GetDirectoryName(dir);
        Assert.NotNull(dir);

        return File.ReadAllText(Path.Combine(dir, "VenueOps", "VenueOps.csproj"));
    }

    private static XDocument LoadAppProject() => XDocument.Parse(LoadAppProjectText());

    private static List<XElement> PackageReferencesNamed(XDocument doc, string include) =>
        doc.Descendants("PackageReference")
            .Where(e => string.Equals((string?)e.Attribute("Include"), include, StringComparison.Ordinal))
            .ToList();

    // QC1 (AC1): exactly one Newtonsoft.Json reference, at 12.0.3, in the package item group.
    [Fact]
    public void AppProject_HasOneNewtonsoftJsonReference_InTheExistingPackageItemGroup()
    {
        XDocument doc = LoadAppProject();

        List<XElement> refs = PackageReferencesNamed(doc, NewtonsoftJson);
        Assert.Single(refs);

        XElement itemGroup = refs[0].Parent!;
        Assert.Equal("ItemGroup", itemGroup.Name.LocalName);
        Assert.Null(itemGroup.Attribute("Condition"));
        Assert.Equal(
            _otherPackagesInItemGroup.Order(StringComparer.Ordinal),
            itemGroup.Elements("PackageReference")
                .Select(e => (string?)e.Attribute("Include") ?? string.Empty)
                .Where(n => n != NewtonsoftJson)
                .Order(StringComparer.Ordinal));
        Assert.Equal("12.0.3", (string?)refs[0].Attribute("Version"));
    }

    // QC2 (AC1): no NU1903 suppression, no NuGet audit property, other versions unchanged.
    [Fact]
    public void AppProject_HasNoAuditSuppressionAndNoNuGetAuditChange()
    {
        string text = LoadAppProjectText();

        Assert.False(text.Contains("NU1903", StringComparison.OrdinalIgnoreCase));
        Assert.False(text.Contains("NuGetAudit", StringComparison.OrdinalIgnoreCase));
        Assert.False(text.Contains("WarningsNotAsErrors", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void AppProject_KeepsTheOtherPackageVersions()
    {
        XDocument doc = LoadAppProject();

        Assert.Equal("14.1.0", (string?)PackageReferencesNamed(doc, "CommunityToolkit.Maui").Single().Attribute("Version"));
        Assert.Equal("8.4.2", (string?)PackageReferencesNamed(doc, "CommunityToolkit.Mvvm").Single().Attribute("Version"));
        Assert.Equal("10.0.6", (string?)PackageReferencesNamed(doc, "Microsoft.Extensions.Http").Single().Attribute("Version"));
        Assert.Equal("10.0.0", (string?)PackageReferencesNamed(doc, "Microsoft.Extensions.Logging.Debug").Single().Attribute("Version"));
        Assert.Equal("10.35.0.4138", (string?)PackageReferencesNamed(doc, "SonarAnalyzer.CSharp").Single().Attribute("Version"));
    }

    // QC3 (AC1): the version is the literal 12.0.3: no range, wildcard, or property indirection.
    [Fact]
    public void NewtonsoftJsonReference_UsesTheLiteralVersion_NotARangeOrProperty()
    {
        XElement reference = PackageReferencesNamed(LoadAppProject(), NewtonsoftJson).Single();

        string? version = (string?)reference.Attribute("Version");
        Assert.Equal("12.0.3", version);
        Assert.NotNull(version);
        Assert.False(version.Contains('['));
        Assert.False(version.Contains('*'));
        Assert.False(version.Contains("$(", StringComparison.Ordinal));
        Assert.Empty(reference.Elements("Version"));
    }
}
