using VenueOps.ViewModels;

namespace VenueOps.Views.Auth;

public partial class LoginView : ContentPage
{
    // Maximum card width on wide screens (tablet landscape / desktop).
    private const double MaxCardWidth = 560;

    public LoginView(LoginViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    // Called by MAUI every time the page is resized (orientation change, window resize, first layout).
    // Sets ContentLayout.WidthRequest = Min(pageWidth, MaxCardWidth) so the card:
    //   • fills the screen on narrow devices / small windows (no clipping)
    //   • caps at MaxCardWidth and centers on wide screens
    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        if (width > 0)
            ContentLayout.WidthRequest = Math.Min(width, MaxCardWidth);
    }
}
