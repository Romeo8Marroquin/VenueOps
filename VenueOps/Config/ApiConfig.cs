namespace VenueOps.Config;

// API base URL is not stored here.
// Create Config/ApiConfig.Local.cs (gitignored) to supply it.
// See ApiConfig.Local.cs.example for the template.
internal static partial class ApiConfig
{
    public static string ApiBaseUrl { get; } = ResolveApiBaseUrl();

    static partial void OverrideApiBaseUrl(ref string url);

    private static string ResolveApiBaseUrl()
    {
        var url = string.Empty;
        OverrideApiBaseUrl(ref url);
        if (string.IsNullOrWhiteSpace(url))
            throw new InvalidOperationException(
                "ApiBaseUrl is not configured. " +
                "Create Config/ApiConfig.Local.cs from the .example template.");
        return url;
    }
}
