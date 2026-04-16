using VenueOps.ViewModels;

namespace VenueOps.Views.Dashboard;

public partial class DashboardView : ContentPage
{
    public DashboardView(DashboardViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
