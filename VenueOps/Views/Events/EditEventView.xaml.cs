using CommunityToolkit.Maui.Views;
using VenueOps.ViewModels;

namespace VenueOps.Views.Events;

public partial class EditEventView : Popup<bool>
{
    public EditEventView(EditEventViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;

        var win = Application.Current?.Windows[0];
        double windowWidth  = win?.Width  ?? 480;
        double windowHeight = win?.Height ?? 800;

        double maxWidth  = Math.Min(windowWidth  * 0.92, 700);
        double maxHeight = Math.Min(windowHeight * 0.88, 750);

        RootBorder.MaximumWidthRequest  = maxWidth;
        RootBorder.MaximumHeightRequest = maxHeight;

        viewModel.IsWideLayout = maxWidth >= 480;
    }
}
