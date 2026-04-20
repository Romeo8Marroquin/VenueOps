using CommunityToolkit.Maui;
using VenueOps.Services;
using VenueOps.ViewModels;
using VenueOps.Views.Auth;
using VenueOps.Views.Dashboard;
using VenueOps.Views.Events;

namespace VenueOps;

public static class MauiProgram
{
    // ── Backend base URL ─────────────────────────────────────────────────────
    private const string ApiBaseUrl = "https://e4a5b3af-81cf-4f20-ba8e-2c41ae080308.mock.pstmn.io/api/";

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
        builder.Services.AddSingleton<IDialogService, DialogService>();
        builder.Services.AddSingleton<ISessionService, SessionService>();
        builder.Services.AddSingleton<IDashboardService, DashboardService>();
        builder.Services.AddSingleton<IEventsService, EventsService>();

        // ── Shell (Singleton) ─────────────────────────────────────────────────
        builder.Services.AddSingleton<AppShell>();
        builder.Services.AddSingleton<AppShellViewModel>();

        // ── ViewModels (Transient) ────────────────────────────────────────────
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<RegisterViewModel>();
        builder.Services.AddTransient<DashboardViewModel>();
        builder.Services.AddTransient<EventsViewModel>();
        builder.Services.AddTransient<CreateEventViewModel>();

        // ── Views (Transient) ─────────────────────────────────────────────────
        builder.Services.AddTransient<LoginView>();
        builder.Services.AddTransient<RegisterView>();
        builder.Services.AddTransient<DashboardView>();
        builder.Services.AddTransient<EventsView>();
        builder.Services.AddTransient<CreateEventView>();

        return builder.Build();
    }
}
