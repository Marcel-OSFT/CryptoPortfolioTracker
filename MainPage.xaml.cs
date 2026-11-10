using CommunityToolkit.Mvvm.Messaging;
using CryptoPortfolioTracker.Models;
using CryptoPortfolioTracker.Reporting.Documents;
using CryptoPortfolioTracker.Reporting.Services;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using QuestPDF.Companion;
using System.Net.Http.Json;
using System.Text.Json;

namespace CryptoPortfolioTracker;
[ObservableObject]
public partial class MainPage : Page, IRecipient<ExchangeRateMissingMessage>
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public static MainPage Current;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public IGraphUpdateService _graphUpdateService;
    public IPriceUpdateService _priceUpdateService;
    private readonly IQuestPdfService _pdfService;

    private readonly Settings _appSettings;
    private Type lastPageType;
    private NavigationViewItem lastSelectedNavigationItem;
    private ILogger Logger { get; set; } = Log.Logger.ForContext(Constants.SourceContextPropertyName, typeof(MainPage).Name.PadRight(22));
    [ObservableProperty] public partial Visibility NavigationVisibility { get; set; }
    [ObservableProperty] public partial bool IsSettingsVisible { get; set; }
    [ObservableProperty] public partial string ExchangeRateText { get; set; } = "Exchange rate: --";
    [ObservableProperty] public partial string ExchangeRateUpdatedText { get; set; } = "Updated: --";
    private string _priceUpdateStatusText = string.Empty;
    public string PriceUpdateStatusText
    {
        get => _priceUpdateStatusText;
        set => SetProperty(ref _priceUpdateStatusText, value);
    }
    //[ObservableProperty]
    private SolidColorBrush _exchangeRateStatusBrush;
    public SolidColorBrush ExchangeRateStatusBrush
    {
        get => _exchangeRateStatusBrush;
        set => SetProperty(ref _exchangeRateStatusBrush, value);
    }

    private readonly IExchangeRateService _exchangeRateService;
    private readonly IMessenger _messenger;
    private readonly IDialogService _dialogService;
    private readonly DispatcherTimer _exchangeRateStatusTimer;
    private readonly DispatcherTimer _notificationTimer;

    public MainPage(IGraphUpdateService graphUpdateService, 
                        IPriceUpdateService priceUpdateService, IMessenger messenger, 
                        IQuestPdfService pdfService, IExchangeRateService exchangeRateService, 
                        IDialogService dialogService ,Settings appSettings)
    {
        _appSettings = appSettings;
        _exchangeRateService = exchangeRateService;
        _messenger = messenger;
        _dialogService = dialogService;
        InitializeComponent();
        Current = this;
        DataContext = this;
        _exchangeRateStatusTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(30)
        };
        _exchangeRateStatusTimer.Tick += (_, _) => UpdateExchangeRateStatus();

        _notificationTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(20)
        };
        _notificationTimer.Tick += (_, _) =>
        {
            NotificationInfoBar.IsOpen = false;
            NotificationInfoBar.Title = string.Empty;
            NotificationInfoBar.Message = string.Empty;
        };

        // Set default value for ExchangeRateStatusBrush
        ExchangeRateStatusBrush = new SolidColorBrush(Colors.Red);

        _exchangeRateService.PropertyChanged += ExchangeRateService_PropertyChanged;
        UpdateExchangeRateStatus();

        _messenger.Register<ExchangeRateMissingMessage>(this);

        _graphUpdateService = graphUpdateService;
        _priceUpdateService = priceUpdateService;
        _pdfService = pdfService;
    }

    public void Receive(ExchangeRateMissingMessage message)
    {
        if (message.Date == default)
        {
            return;
        }
        ShowTransientNotification("Capital Flow calculation warning.", message.Reason, InfoBarSeverity.Warning);
    }

    private void ExchangeRateService_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(IExchangeRateService.EurUsdRate) or nameof(IExchangeRateService.LastUpdated) or nameof(IExchangeRateService.LastUpdatedDisplay))
        {
            UpdateExchangeRateStatus();
        }
    }

    private void ShowTransientNotification(string title, string message, InfoBarSeverity severity)
    {
        NotificationInfoBar.Title = title;
        NotificationInfoBar.Message = message;
        NotificationInfoBar.Severity = severity;
        NotificationInfoBar.IsOpen = true;

        _notificationTimer.Stop();
        _notificationTimer.Start();
    }

    private void UpdateExchangeRateStatus()
    {
        bool isFresh = _exchangeRateService.LastUpdated != default &&
            DateTimeOffset.UtcNow - _exchangeRateService.LastUpdated < TimeSpan.FromMinutes(15);

        if (_exchangeRateService.EurUsdRate <= 0)
        {
            ExchangeRateText = "Exchange rate: unavailable";
            ExchangeRateUpdatedText = $"Updated: {_exchangeRateService.LastUpdatedDisplay}";
            ExchangeRateStatusBrush = isFresh ? new SolidColorBrush(Colors.Green) : new SolidColorBrush(Colors.Red);
            return;
        }

        ExchangeRateText = $"1 EUR = {_exchangeRateService.EurUsdRate:F4} USD";
        ExchangeRateUpdatedText = $"Updated: {_exchangeRateService.LastUpdatedDisplay}";
        ExchangeRateStatusBrush = isFresh ? new SolidColorBrush(Colors.Green) : new SolidColorBrush(Colors.Red);
    }

    private async void MainPage_Loaded(object sender, RoutedEventArgs e)
    {
        _dialogService.XamlRoot = this.XamlRoot;
        _exchangeRateStatusTimer.Start();

        navigationView.SelectedItem = navigationView.MenuItems.OfType<NavigationViewItem>().Where(x => x.Name == "AssetsView").First();
        App.Splash?.Close();
        App.Splash = null;

        await ShowWhatsNewDialogIfNeeded();

        _graphUpdateService.Start();
        _priceUpdateService.Start();

        // Hide items if duress mode is enabled
        if (App.IsDuressMode)
        {
            NavigationVisibility = Visibility.Collapsed;
            IsSettingsVisible = false;
        }
        else
        {
            NavigationVisibility = Visibility.Visible;
            IsSettingsVisible = true;
            if (_appSettings.IsCheckForUpdate)
            {
                await CheckUpdateNow();
            }
        }
    }

    private async Task ShowWhatsNewDialogIfNeeded()
    {
        string currentVersion = AppConstants.ProductVersion ?? "";
        string lastVersion = _appSettings.LastVersion;

        if (!string.Equals(currentVersion, lastVersion, StringComparison.OrdinalIgnoreCase))
        {
            //var dialog = new WhatsNewDialog(_appSettings);
            //dialog.XamlRoot = MainPage.Current.XamlRoot;
            //await dialog.ShowAsync();

            await _dialogService.ShowAsync<WhatsNewDialog, bool>();

            _appSettings.LastVersion = currentVersion;
        }
    }

    public async Task CheckUpdateNow()
    {
        Logger.Information("Checking for updates");
        var loc = Localizer.Get();
        AppUpdater appUpdater = new();
        var result = await appUpdater.Check(AppConstants.VersionUrl, AppConstants.ProductVersion);

        if (result == AppUpdaterResult.NeedUpdate)
        {
            Logger.Information("Update Available");

            var dlgResult = await _dialogService.ShowMessageAsync(
                loc.GetLocalizedString("Messages_UpdateChecker_NewVersionTitle"),
                loc.GetLocalizedString("Messages_UpdateChecker_NewVersionMsg"),
                loc.GetLocalizedString("Common_DownloadButton"),
                loc.GetLocalizedString("Common_CancelButton"));


            if (dlgResult == ContentDialogResult.Primary)
            {
                Logger.Information("Downloading update");
                var downloadResult = await appUpdater.DownloadSetupFile();
                //Download is running async, so the user can continue to do other stuff
                if (downloadResult == AppUpdaterResult.DownloadSuccesfull)
                {
                    //*** Download is doen, wait till there is no other dialog box open
                    await App.DialogCompletionTask;
                    Logger.Information("Download Succesfull");
                    var installRequest = await _dialogService.ShowMessageAsync(
                        loc.GetLocalizedString("Messages_UpdateChecker_DownloadSuccesTitle"),
                        loc.GetLocalizedString("Messages_UpdateChecker_DownloadSuccesMsg"),
                        loc.GetLocalizedString("Common_InstallButton"),
                        loc.GetLocalizedString("Common_CancelButton"));
                    if (installRequest == ContentDialogResult.Primary)
                    {
                        Logger.Information("Closing Application and Installing Update");
                        appUpdater.ExecuteSetupFile();
                    }
                }
                else
                {
                    Logger.Warning("Download failed");
                    await _dialogService.ShowMessageAsync(
                        loc.GetLocalizedString("Messages_UpdateChecker_DownloadFailedTitle"),
                        loc.GetLocalizedString("Messages_UpdateChecker_DownloadFailedMsg"),
                        loc.GetLocalizedString("Common_CloseButton"));
                }
            }
        }
    }

    private async void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        var selectedItem = (NavigationViewItem)args.SelectedItem;
        Type? pageType;
        if (args.IsSettingsSelected)
        {
            pageType = Type.GetType("CryptoPortfolioTracker.Views.SettingsView");
            if (pageType != null && pageType != lastPageType)
            {
                LoadView(pageType);
                lastSelectedNavigationItem = selectedItem;
                Logger.Information("Navigated to {0}", (string)selectedItem.Tag);
            }
        }
        else if (selectedItem != null)
        {
            pageType = Type.GetType("CryptoPortfolioTracker.Views." + (string)selectedItem.Tag);
            if (pageType is not null && pageType != lastPageType)
            {
                LoadView(pageType);
                lastSelectedNavigationItem = selectedItem;
                Logger.Information("Navigated to {0}", (string)selectedItem.Tag);
            }
            else if (pageType is null)
            {

                switch ((string)selectedItem.Tag)
                {
                    case "Exit":
                        {
                            Logger.Information("Application Exit");
                            Environment.Exit(0);
                            break;
                        }
                    case "Help":
                        {
                            DisplayHelpFile();
                            Logger.Information("Help File Requested");
                            break;
                        }
                    case "WhatsNew":
                        {
                            DisplayWhatsNewFile();
                            Logger.Information("What's New File Requested");
                            break;
                        }
                    case "About":
                        {
                            //var dialog = new AboutDialog();
                            //dialog.XamlRoot = MainPage.Current.XamlRoot;
                            //var result = await dialog.ShowAsync();

                            await _dialogService.ShowAsync<AboutDialog, bool>();

                            break;
                        }
                }

                navigationView.SelectedItem = lastSelectedNavigationItem;
            }
        }
    }
    private void LoadView(Type pageType)
    { 

        if (pageType.Name == "CoinLibraryView")
        {
            _graphUpdateService.Pause();
            _priceUpdateService.Pause();
            UpdatePriceUpdateStatus(true);
        }
        else if (lastPageType is not null && (lastPageType.Name == "CoinLibraryView"))
        {
            if (_graphUpdateService != null)
            {
                _graphUpdateService.Resume();
            }
            else
            {
                _graphUpdateService = App.Container.GetService<IGraphUpdateService>();
                _graphUpdateService?.Start();
            }

            if (_priceUpdateService != null)
            {
                _priceUpdateService.Resume();
            }
            else
            {
                _priceUpdateService = App.Container.GetService<IPriceUpdateService>();
                _priceUpdateService?.Start();
            }

            UpdatePriceUpdateStatus(false);
        }
        else
        {
            UpdatePriceUpdateStatus(false);
        }

        lastPageType = pageType;
        contentFrame.Content = App.Container.GetService(pageType);
    }

    private void UpdatePriceUpdateStatus(bool isPaused)
    {
        PriceUpdateStatusText = isPaused
            ? "Price updates paused while Coin Library is open to avoid CoinGecko API conflicts."
            : string.Empty;
    }

    private async void DisplayHelpFile()
    {
        var loc = Localizer.Get();
        var fileName = "HelpFile_NL.pdf";

        if (string.Equals(_appSettings.AppCultureLanguage, "en-US", StringComparison.OrdinalIgnoreCase))
        {
            fileName = "HelpFile_EN.pdf";
        }
        try
        {
            Process.Start(new ProcessStartInfo(AppConstants.Url + fileName) { UseShellExecute = true });
            Logger.Information("HelpFile Displayed");
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "Failed to display HelpFile");
            await _dialogService.ShowMessageAsync(
                loc.GetLocalizedString("Messages_HelpFile_FailedTitle"),
                loc.GetLocalizedString("Messages_HelpFile_FailedMsg"),
                loc.GetLocalizedString("Common_CloseButton"));
        }
    }
    private async void DisplayWhatsNewFile()
    {
        var loc = Localizer.Get();
        var fileName = Path.Combine("docs","WhatsNew_NL.pdf");

        if (string.Equals(_appSettings.AppCultureLanguage, "en-US", StringComparison.OrdinalIgnoreCase))
        {
            fileName = Path.Combine("docs","WhatsNew_EN.pdf");
        }
        try
        {
            Process.Start(new ProcessStartInfo(AppConstants.Url + fileName) { UseShellExecute = true });
            Logger.Information("What's New File Displayed");
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "Failed to display What's New File");
            await _dialogService.ShowMessageAsync(
                loc.GetLocalizedString("Messages_WhatsNewFile_FailedTitle"),
                loc.GetLocalizedString("Messages_WhatsNewFile_FailedMsg"),
                loc.GetLocalizedString("Common_CloseButton"));
        }
    }


    private async void DisplayTestPDF()
    {
        try
        {
            //var document = Document.Create(container =>
            //{
            //    container.Page(page =>
            //    {
            //        page.Size(PageSizes.A4);
            //        page.Margin(2, Unit.Centimetre);
            //        page.DefaultTextStyle(x => x.FontSize(20));
            //        page.Header().Text("Test123").SemiBold().FontSize(36).FontColor(Colors.Blue.Medium);
            //        page.Content().PaddingVertical(1, Unit.Centimetre).Column(x =>
            //        {
            //            x.Spacing(20);
            //            x.Item().Text(Placeholders.LoremIpsum());
            //            x.Item().Image(Placeholders.Image(200, 110));
            //        });
            //        page.Footer().AlignCenter().Text(x => { x.Span("Page "); x.CurrentPageNumber(); });
            //    });
            //});


            //// Create document instance
            var doc = new TestDocument("What's New");
            doc.ShowInCompanionAsync();

            ////Render & save off UI thread inside service
            //var outputPath = Path.Combine(AppConstants.AppDataPath, "Reports", "TestDocument.pdf");
            //await _pdfService.SaveAsync(doc, outputPath);
            //Process.Start(new ProcessStartInfo(outputPath) { UseShellExecute = true });
        }
        catch (Exception ex) 
        {
            Debug.WriteLine(ex);
        }
    }


}

