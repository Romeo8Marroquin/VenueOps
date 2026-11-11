namespace VenueOps.Services;

public interface IShareService
{
    Task ShareTextAsync(string title, string text);
}
