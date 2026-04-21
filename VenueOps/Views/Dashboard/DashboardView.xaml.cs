using VenueOps.ViewModels;

namespace VenueOps.Views.Dashboard;

public partial class DashboardView : ContentPage
{
    private readonly DashboardViewModel _viewModel;

    public DashboardView(DashboardViewModel viewModel)
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

    /// <summary>
    /// Switches the stats grid between 3 columns × 2 rows (tablet / desktop)
    /// and 2 columns × 3 rows (small phone portrait) at a 480dp breakpoint.
    /// Called by MAUI on every layout pass; the early-exit guard keeps it cheap.
    /// </summary>
    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);

        if (width <= 0 || double.IsInfinity(width) || double.IsNaN(width)) return;

        _viewModel.IsWideLayout = width >= 600;

        int targetCols = width < 480 ? 2 : 3;

        // Nothing to do if the column count already matches the target
        if (StatsGrid.ColumnDefinitions.Count == targetCols) return;

        // Rebuild column definitions
        StatsGrid.ColumnDefinitions.Clear();
        for (int i = 0; i < targetCols; i++)
            StatsGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

        // Rebuild row definitions
        int targetRows = targetCols == 3 ? 2 : 3;
        StatsGrid.RowDefinitions.Clear();
        for (int i = 0; i < targetRows; i++)
            StatsGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

        // Cell positions indexed by stat card order:
        //   0=TotalEvents, 1=Bookings, 2=Attendees, 3=ActiveVenues, 4=Occupancy, 5=Sponsors
        (int Row, int Col)[] positions = targetCols == 3
            ? [(0, 0), (0, 1), (0, 2), (1, 0), (1, 1), (1, 2)]
            : [(0, 0), (0, 1), (1, 0), (1, 1), (2, 0), (2, 1)];

        var children = StatsGrid.Children.ToList();
        for (int i = 0; i < Math.Min(children.Count, positions.Length); i++)
        {
            if (children[i] is BindableObject cell)
            {
                Grid.SetRow(cell, positions[i].Row);
                Grid.SetColumn(cell, positions[i].Col);
            }
        }
    }
}
