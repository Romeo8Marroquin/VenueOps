namespace VenueOps.Services;

/// <summary>Opens the platform share sheet so view models can share text and stay unit testable.</summary>
public interface IShareService
{
    Task ShareTextAsync(string title, string text);
}
