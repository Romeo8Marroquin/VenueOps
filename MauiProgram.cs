using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using VenueOps.Services;
using VenueOps.ViewModels;
using VenueOps.Views.Auth;
using VenueOps.Views.Dashboard;

namespace VenueOps;

public static class MauiProgram
{
    // ── Backend base URL ─────────────────────────────────────────────────────
    // Change this constant to point to the real API environment.
    // The trailing slash is required for relative path resolution with HttpClient.
    private const string ApiBaseUrl = "https://e4a5b3af-81cf-4f20-ba8e-2c41ae080308.mock.pstmn.io/api/";
    // ────────────────────────────────────────────────────────────────────────

    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // ── HTTP ──────────────────────────────────────────────────────────────
        builder.Services.AddHttpClient("VenueOpsApi", client =>
        {
            client.BaseAddress = new Uri(ApiBaseUrl);
        });

        // ── Services (Singleton) ──────────────────────────────────────────────
        builder.Services.AddSingleton<IAuthService, AuthService>();
        builder.Services.AddSingleton<INavigationService, NavigationService>();

        // ── Shell (Singleton) ─────────────────────────────────────────────────
        builder.Services.AddSingleton<AppShell>();

        // ── ViewModels (Transient) ────────────────────────────────────────────
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<DashboardViewModel>();

        // ── Views (Transient) ─────────────────────────────────────────────────
        builder.Services.AddTransient<LoginView>();
        builder.Services.AddTransient<DashboardView>();

        // ── Entry handler — remove platform-native borders / underlines ───────
        // Our Border wrapper is the sole visual frame for input fields.
        // Without this, Android adds a Material underline and Windows adds its
        // own rounded-rect border, producing an unwanted double-frame effect.
        ConfigureEntryHandler();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }

    private static void ConfigureEntryHandler()
    {
        Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("VenueOpsEntry", (handler, _) =>
        {
#if ANDROID
            // Remove Material Design bottom-line / background tint
            handler.PlatformView.BackgroundTintList =
                Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
            handler.PlatformView.SetBackgroundColor(Android.Graphics.Color.Transparent);

#elif IOS || MACCATALYST
            // iOS Entry is borderless by default in MAUI, but set explicitly for safety
            handler.PlatformView.BorderStyle = UIKit.UITextBorderStyle.None;

#elif WINDOWS
            // Remove WinUI TextBox border and focus visuals so our Border is the frame
            handler.PlatformView.BorderThickness = new Microsoft.UI.Xaml.Thickness(0);
            handler.PlatformView.FocusVisualPrimaryThickness = new Microsoft.UI.Xaml.Thickness(0);
            handler.PlatformView.FocusVisualSecondaryThickness = new Microsoft.UI.Xaml.Thickness(0);
            handler.PlatformView.Background =
                new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);
#endif
        });
    }
}
