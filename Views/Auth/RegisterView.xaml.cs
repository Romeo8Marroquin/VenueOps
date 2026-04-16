using VenueOps.ViewModels;

namespace VenueOps.Views.Auth;

public partial class RegisterView : ContentPage
{
    private const double MaxCardWidth = 560;

    public RegisterView(RegisterViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        if (width > 0)
            ContentLayout.WidthRequest = Math.Min(width, MaxCardWidth);
    }
}
