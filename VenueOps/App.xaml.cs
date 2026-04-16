using VenueOps.Views.Auth;

namespace VenueOps;

public partial class App : Application
{
    private readonly IServiceProvider _serviceProvider;

    public App(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        // InitializeComponent() processes App.xaml and loads all merged
        // ResourceDictionaries (Colors.xaml, Styles.xaml) into the app resources.
        // This MUST happen before any page calls InitializeComponent() and tries
        // to resolve StaticResource keys defined in those dictionaries.
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // Called by the MAUI platform AFTER our constructor completes,
        // so App.xaml resources are fully loaded before LoginView is created.
        // LoginView is Transient → fresh ViewModel state on every call.
        return new Window(_serviceProvider.GetRequiredService<LoginView>());
    }
}
