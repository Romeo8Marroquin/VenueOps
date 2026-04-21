using VenueOps.ViewModels;

namespace VenueOps.Views.Events;

public partial class EventsView : ContentPage
{
    private readonly EventsViewModel _viewModel;

    public EventsView(EventsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = _viewModel.InitializeAsync();
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);

        if (width <= 0 || double.IsInfinity(width) || double.IsNaN(width)) return;

        _viewModel.IsWideLayout = width >= 600;
    }
}
