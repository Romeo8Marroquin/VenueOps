namespace VenueOps.Config;

// API base URL is not stored here.
// Create Config/ApiConfig.Local.cs (gitignored) to supply it.
// See ApiConfig.Local.cs.example for the template.
[System.Diagnostics.CodeAnalysis.SuppressMessage("Major Bug", "S3251:Implementations should be provided for \"partial\" methods",
    Justification = "OverrideApiBaseUrl is implemented in the gitignored, machine-local ApiConfig.Local.cs; without it the app stops with a clear message.")]
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
