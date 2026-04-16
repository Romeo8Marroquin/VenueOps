using VenueOps.Views.Dashboard;

namespace VenueOps;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Register routes for programmatic Shell.GoToAsync navigation.
        // LoginView lives outside the Shell hierarchy — it is shown by
        // switching Application.MainPage via INavigationService, not GoToAsync.
        Routing.RegisterRoute("dashboard", typeof(Views.Dashboard.DashboardView));
    }
}
