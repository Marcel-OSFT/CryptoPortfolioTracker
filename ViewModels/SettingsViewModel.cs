using System.Security.Cryptography;
using Windows.Security.Credentials;
using Microsoft.UI.Xaml;

namespace TemperatureMonitor.ViewModels;

public partial class SettingsViewModel : BaseViewModel, INotifyPropertyChanged
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public static SettingsViewModel Current;
    public Settings AppSettings => base.AppSettings; // expose AppSettings publicly so that it can be used in dialogs called by this ViewModel


#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.


    [ObservableProperty]
    private ElementTheme appTheme;
    partial void OnAppThemeChanged(ElementTheme value) => AppSettings.AppTheme = value;

    // index property to make ComboBox binding straightforward in XAML
    [ObservableProperty]
    private int appThemeIndex;
    partial void OnAppThemeIndexChanged(int value)
    {
        // map index -> ElementTheme
        AppTheme = value switch
        {
            1 => ElementTheme.Light,
            2 => ElementTheme.Dark,
            _ => ElementTheme.Default
        };
        // persist via AppTheme property setter
    }

    [ObservableProperty]
    private int numberFormatIndex;
    partial void OnNumberFormatIndexChanged(int value) => SetNumberSeparatorsFromIndex(value);

    [ObservableProperty]
    private int appCultureIndex;
    partial void OnAppCultureIndexChanged(int value) => SetCulturePreferenceFromIndex(value);

    [ObservableProperty]
    private double fontSize;
    partial void OnFontSizeChanged(double value) => AppSettings.FontSize = (AppFontSize)value;

    [ObservableProperty]
    private int sampleIntervalFastSeconds;
    partial void OnSampleIntervalFastSecondsChanged(int value) => AppSettings.SampleIntervalFastSeconds = (int)value;
    
    [ObservableProperty]
    private int sampleIntervalSlowMinutes;
    partial void OnSampleIntervalSlowMinutesChanged(int value) => AppSettings.SampleIntervalSlowMinutes = (int)value;

    // New: manual ESP IP address exposed to the View
    [ObservableProperty]
    private string espIpAddress;
    partial void OnEspIpAddressChanged(string value) => AppSettings.EspIpAddress = value;



    public SettingsViewModel(Settings appSettings) : base(appSettings)
    {
        Current = this;

        InitializeFields();
    }

    private void InitializeFields()
    {
        FontSize = (double)AppSettings.FontSize;
        AppTheme = AppSettings.AppTheme;
        // set the index so the ComboBox reflects current theme
        AppThemeIndex = AppSettings.AppTheme switch
        {
            ElementTheme.Light => 1,
            ElementTheme.Dark => 2,
            _ => 0
        };

        NumberFormatIndex = AppSettings.NumberFormat.NumberDecimalSeparator == "," ? 0 : 1;
        AppCultureIndex = AppSettings.AppCultureLanguage[..2].ToLower() == "nl" ? 0 : 1;
        SampleIntervalSlowMinutes = AppSettings.SampleIntervalSlowMinutes;
        SampleIntervalFastSeconds = AppSettings.SampleIntervalFastSeconds;
        // initialize new ESP IP field
        EspIpAddress = AppSettings.EspIpAddress;
    }


    private void SetCulturePreferenceFromIndex(int index)
    {
        string language = index == 0 ? "nl" : "en-US";
        AppSettings.AppCultureLanguage = language;
    }

    private void SetNumberSeparatorsFromIndex(int index)
    {
        var nf = new NumberFormatInfo
        {
            NumberDecimalSeparator = index == 0 ? "," : ".",
            NumberGroupSeparator = index == 0 ? "." : ","
        };
        AppSettings.NumberFormat = nf;
    }




}