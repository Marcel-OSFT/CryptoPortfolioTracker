namespace TemperatureMonitor;
[ObservableObject]
public partial class MainPage : Page //INotifyPropertyChanged
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public static MainPage Current;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private readonly Settings _appSettings;
    private IGraphUpdateService _graphUpdateService;
    private Type lastPageType;
    private NavigationViewItem lastSelectedNavigationItem;

    public MainPage(IGraphUpdateService graphUpdateService, Settings appSettings)
    {
        _appSettings = appSettings;
        InitializeComponent();
        Current = this;
        DataContext = this;
        _graphUpdateService = graphUpdateService;

    }

    private async void MainPage_Loaded(object sender, RoutedEventArgs e)
    {
        //navigationView.SelectedItem = navigationView.MenuItems.OfType<NavigationViewItem>().Where(x => Tag == "DashboardView").First();
        navigationView.SelectedItem = navigationView.MenuItems.First();
    }

    
    private async void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        var selectedItem = (NavigationViewItem)args.SelectedItem;
        Type? pageType;
        if (args.IsSettingsSelected)
        {
            pageType = Type.GetType("TemperatureMonitor.Views.SettingsView");
            if (pageType != null && pageType != lastPageType)
            {
                LoadView(pageType);
                lastSelectedNavigationItem = selectedItem;
            }
        }
        else if (selectedItem != null)
        {
            pageType = Type.GetType("TemperatureMonitor.Views." + (string)selectedItem.Tag);
            if (pageType is not null && pageType != lastPageType)
            {
                LoadView(pageType);
                lastSelectedNavigationItem = selectedItem;
            }
            else if (pageType is null)
            {
                navigationView.SelectedItem = lastSelectedNavigationItem;
            }
        }
    }
    private void LoadView(Type pageType)
    {
        lastPageType = pageType;
        contentFrame.Content = App.Container.GetService(pageType);
        _graphUpdateService.StartAsync();
    }

    public async Task<ContentDialogResult> ShowMessageDialog(string title, string message, string primaryButtonText = "OK", string closeButtonText = "")
    {
        var dialog = new ContentDialog()
        {
            Title = title,
            XamlRoot = Current.XamlRoot,
            Content = message,
            PrimaryButtonText = primaryButtonText,
            CloseButtonText = closeButtonText,
            RequestedTheme = _appSettings.AppTheme
        };
        var dlgResult = await dialog.ShowAsync();
        return dlgResult;
    }

}

