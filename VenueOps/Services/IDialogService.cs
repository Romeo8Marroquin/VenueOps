namespace VenueOps.Services;

public interface IDialogService
{
    Task ShowAlertAsync(string title, string message, string buttonText);
}
