using VenueOps.ViewModels;

namespace VenueOps;

public partial class AppShell : Shell
{
    public AppShell(AppShellViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;

        // Register routes for programmatic Shell.GoToAsync navigation.
        // LoginView lives outside the Shell hierarchy — it is shown by
        // switching Window[0].Page via INavigationService, not GoToAsync.
        Routing.RegisterRoute("dashboard", typeof(Views.Dashboard.DashboardView));
        Routing.RegisterRoute("events", typeof(Views.Events.EventsView));
        Routing.RegisterRoute("eventDetail", typeof(Views.Events.EventDetailView));
    }
}
