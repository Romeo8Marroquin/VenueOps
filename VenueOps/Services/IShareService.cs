namespace VenueOps.Services;

/// <summary>Opens the platform share sheet with plain text.</summary>
public interface IShareService
{
    Task ShareTextAsync(string title, string text);
}
