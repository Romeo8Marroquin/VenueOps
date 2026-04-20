using CommunityToolkit.Maui.Views;
using VenueOps.ViewModels;

namespace VenueOps.Views.Events;

public partial class CreateEventView : Popup<bool>
{
    public CreateEventView(CreateEventViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;

        // Size the popup relative to the current window.
        // Wide windows get a two-column form; narrow windows get single-column.
        var win = Application.Current?.Windows[0];
        double windowWidth  = win?.Width  ?? 480;
        double windowHeight = win?.Height ?? 800;

        double popupWidth  = windowWidth  > 600
            ? Math.Min(700, windowWidth  - 48)
            : windowWidth - 32;

        double popupHeight = windowHeight > 700
            ? Math.Min(750, windowHeight - 80)
            : windowHeight * 0.9;
        HeightRequest = popupHeight;
        WidthRequest = popupWidth;

        DialogCard.WidthRequest = popupWidth;
        DialogCard.MaximumWidthRequest = popupWidth;
        DialogCard.HeightRequest = popupHeight;
        DialogCard.MaximumHeightRequest = popupHeight;

        viewModel.IsWideLayout = popupWidth >= 480;
    }
}
