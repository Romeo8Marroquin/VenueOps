using VenueOps.Services;
using VenueOps.Tests.Support;

namespace VenueOps.Tests.Services;

public class ShareServiceTests
{
    [Fact]
    public void ShareService_ImplementsIShareService()
    {
        Assert.True(typeof(IShareService).IsAssignableFrom(typeof(ShareService)));
    }

    [Fact]
    public void ShareService_WrapsShareDefaultWithTitleAndPlainTextOnly()
    {
        // Share.Default needs a running MAUI app, so the wrapper is checked as source text.
        string source = RepositoryFile.ReadAllText("VenueOps/Services/ShareService.cs");

        Assert.Matches(
            @"Share\.Default\.RequestAsync\(\s*new ShareTextRequest\s*\{\s*Title\s*=\s*title,\s*Text\s*=\s*text\s*\}\s*\)",
            source);
        Assert.DoesNotMatch(@"ShareFileRequest|ShareMultipleFilesRequest|\bUri\b|\bFiles\b", source);
    }
}
