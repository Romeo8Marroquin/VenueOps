namespace VenueOps.Services;

/// <summary>
/// Thin wrapper over the platform share sheet so view models can share text without
/// touching MAUI statics and stay unit testable.
/// </summary>
public sealed class ShareService : IShareService
{
    public Task ShareTextAsync(string title, string text)
        => Microsoft.Maui.ApplicationModel.DataTransfer.Share.Default.RequestAsync(
            new Microsoft.Maui.ApplicationModel.DataTransfer.ShareTextRequest { Title = title, Text = text });
}
