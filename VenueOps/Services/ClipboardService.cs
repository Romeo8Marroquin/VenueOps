namespace VenueOps.Services;

/// <summary>
/// Thin wrapper over the platform clipboard so view models can copy text without
/// touching MAUI statics and stay unit testable.
/// </summary>
public sealed class ClipboardService : IClipboardService
{
    public Task SetTextAsync(string text)
        => Microsoft.Maui.ApplicationModel.DataTransfer.Clipboard.Default.SetTextAsync(text);
}
