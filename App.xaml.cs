using CommunityToolkit.Mvvm.Messaging;
using TemperatureMonitor.Converters;

using Task = System.Threading.Tasks.Task;

namespace TemperatureMonitor;

public partial class App : Application
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    private Mutex _mutex;
    private const string MutexName = "MyTempMonitorMutex";
    public static readonly SemaphoreSlim UpdateSemaphore = new SemaphoreSlim(1, 1);
    private static Settings _appSettings { get; set; }
    public static App Current { get; private set; }
    public static MainWindow? Window { get; private set; }
    public static ILocalizer? Localizer { get; private set; }
    public static IServiceProvider Container { get; private set; }

    private static TaskCompletionSource<bool>? dialogCompletionSource; // = new TaskCompletionSource<bool>();
    public static Task DialogCompletionTask => dialogCompletionSource?.Task ?? Task.CompletedTask;
    public List<DataPoint> currentDayTemperatures { get; set; }




    public App()
    {
        Current = this;
        InitializeComponent();
        this.UnhandledException += OnUnhandledException;

        AppConstants.GetAppEnvironmentals();
        Container = RegisterServices();
        _appSettings = Container.GetRequiredService<Settings>();
    }

    protected async override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        if (!await EnsureSingleInstanceAsync()) return;
        await InitializeLocalizer();

        //await MockLogGenerator.CreateMockLogFileAsync(Path.Combine(AppConstants.AppDataPath,"mocklog.txt"));
        List<DataPoint> allTemperaturesOnMultipleDays = await MockLogReader.ReadMockLogFileAsync(Path.Combine(AppConstants.AppDataPath, "mocklog.txt"));
        // archive all readings into daily files under AppConstants.AppDataPath\DailyArchive
        await DailyArchiveService.ArchiveAsync(allTemperaturesOnMultipleDays, AppConstants.AppDataPath, useLocalDate: true);
        


        Window = Container.GetService<MainWindow>();
        Window?.Activate();
    }
    
    private async Task<bool> EnsureSingleInstanceAsync()
    {
        try
        {
            _mutex = new Mutex(false, MutexName, out bool createdNew);

            if (!createdNew && !AdminCheck.IsRunAsAdmin())
            {
                await ShowErrorMessage("Another instance of the application is already running.");
                _mutex.Close();
                _mutex = null;
                Application.Current.Exit();
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            var dialog = new ContentDialog
            {
                Title = "Error",
                Content = $"Failed to check for single instance: {ex.Message}",
                CloseButtonText = "OK"
            };

            await dialog.ShowAsync();
            return false;
        }
    }

    private async Task InitializeLocalizer()
    {
        var stringsFolderPath = Path.Combine(AppContext.BaseDirectory, "Strings");

        Localizer = await new LocalizerBuilder()
            .AddStringResourcesFolderForLanguageDictionaries(stringsFolderPath)
            .Build();

        var culture = _appSettings.AppCultureLanguage;

        try
        {
            await Localizer.SetLanguage(culture);
        }
        catch (Exception ex)
        {
        }
    }
    
    private static IServiceProvider RegisterServices()
    {
        var services = new ServiceCollection();

        services.AddScoped<SettingsView>();
        services.AddScoped<MainPage>();
        services.AddScoped<MainWindow>();
        services.AddScoped<DashboardView>();

        services.AddScoped<DashboardViewModel>();
        services.AddScoped<SettingsViewModel>();
        services.AddScoped<BaseViewModel>();

        services.AddSingleton<IGraphService, GraphService>();
        services.AddSingleton<IGraphUpdateService, GraphUpdateService>();
        services.AddSingleton<IPreferenceStore, FilePreferenceStore>();

        services.AddSingleton<Settings>();
        services.AddSingleton<Esp32Service>();
        services.AddSingleton<DateToStringConverter>(); // converter instance

        services.AddSingleton<IMessenger, WeakReferenceMessenger>();

        return services.BuildServiceProvider();
    }

    public void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {

        // Prevent the application from crashing
        e.Handled = true;

        // Show a user-friendly message
        _ = ShowErrorMessage(e.Message);
    }

    public static async Task ShowErrorMessage(string message)
    {
        var xamlRoot = MainPage.Current?.XamlRoot;
        if (xamlRoot != null)
        {
            var dialog = new ContentDialog
            {
                Title = "Error",
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = xamlRoot
            };

            await dialog.ShowAsync();
        }
    }

    public static async Task<ContentDialogResult> ShowMessageDialog(string title, string message, string primaryButtonText = "OK", string closeButtonText = "")
    {
        var xamlRoot = MainPage.Current?.XamlRoot;
        var result = ContentDialogResult.None;
        if (xamlRoot != null)
        {
            var dialog = new ContentDialog()
            {
                Title = title,
                XamlRoot = xamlRoot,
                Content = message,
                PrimaryButtonText = primaryButtonText,
                CloseButtonText = closeButtonText,
                RequestedTheme = _appSettings.AppTheme
            };

            result = await ShowContentDialogAsync(dialog);
        }
        return result;
    }

    public static async Task<ContentDialogResult> ShowContentDialogAsync(ContentDialog dialog)
    {
        dialogCompletionSource = new TaskCompletionSource<bool>();

        dialog.Closed += (s, e) => dialogCompletionSource.TrySetResult(true);

        var result = await dialog.ShowAsync();

        // Ensure the completion source is set in case Closed wasn't triggered
        if (!dialogCompletionSource.Task.IsCompleted)
            dialogCompletionSource.TrySetResult(true);

        return result;
    }

    

}
