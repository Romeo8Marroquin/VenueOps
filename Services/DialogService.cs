namespace VenueOps.Services;

/// <summary>
/// Cross-platform dialog service.
/// Resolves the current page at call-time via Application.Current.Windows[0].Page
/// so no Page reference is stored and navigation changes are handled automatically.
/// Uses Page.DisplayAlertAsync — the non-deprecated MAUI .NET 10 replacement for
/// the obsolete Page.DisplayAlert.
/// </summary>
public sealed class DialogService : IDialogService
{
    public Task ShowAlertAsync(string title, string message, string buttonText)
    {
        var page = Application.Current?.Windows[0].Page;
        return page is not null
            ? page.DisplayAlertAsync(title, message, buttonText)
            : Task.CompletedTask;
    }
}
