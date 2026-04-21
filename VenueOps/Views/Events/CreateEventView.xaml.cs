using CommunityToolkit.Maui.Views;
using VenueOps.ViewModels;

namespace VenueOps.Views.Events;

public partial class CreateEventView : Popup<bool>
{
    public CreateEventView(CreateEventViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;

        var win = Application.Current?.Windows[0];
        double windowWidth  = win?.Width  ?? 480;
        double windowHeight = win?.Height ?? 800;

        double popupWidth = windowWidth > 600
            ? Math.Min(700, windowWidth - 48)
            : windowWidth - 32;

        // The Border grows to fit its content naturally.
        // MaximumHeightRequest caps it so the whole modal scrolls as one unit
        // when the window is too small — no forced height, no empty whitespace.
        double maxWidth  = Math.Min(windowWidth  * 0.92, 700);
        double maxHeight = Math.Min(windowHeight * 0.88, 750);

        RootBorder.MaximumWidthRequest  = maxWidth;
        RootBorder.MaximumHeightRequest = maxHeight;

        viewModel.IsWideLayout = maxWidth >= 480;
    }
}
