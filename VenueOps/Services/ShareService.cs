namespace VenueOps.Services;

public class ShareService : IShareService
{
    public Task ShareTextAsync(string title, string text) =>
        Share.Default.RequestAsync(new ShareTextRequest { Title = title, Text = text });
}
