
using CryptoPortfolioTracker.Models;
using Microsoft.UI.Xaml;
using Serilog;
using System.IO;
using System.Xml.Serialization;

using Serilog.Core;
using System.Globalization;
using CryptoPortfolioTracker.Enums;


namespace CryptoPortfolioTracker.Services;

public class PreferencesService : IPreferencesService
{
    private UserPreferences userPreferences;
    private Serilog.ILogger Logger { get; set; }
    public PreferencesService() 
    {
        userPreferences = new UserPreferences();
    }

    public UserPreferences GetPreferences()
    {
        return userPreferences;
    }

    public void SetNumberFormat(NumberFormatInfo nf)
    {
        userPreferences.NumberFormat = nf;
        SaveUserPreferences("NumberFormat", nf);
    }
    
    public void SetAppCultureLanguage(string language)
    {
        userPreferences.AppCultureLanguage = language;
        if (App.Localizer == null)
        {
            return;
        }

        App.Localizer.SetLanguage(userPreferences.AppCultureLanguage);
        SaveUserPreferences("AppCultureLanguage", language);
    }
    public void SetFontSize(AppFontSize fontSize)
    {
        userPreferences.FontSize = fontSize;
        SaveUserPreferences("FontSize", fontSize);
    }
    public void SetAppTheme(ElementTheme theme)
    {
        userPreferences.AppTheme = theme;

        if (App.Window != null && App.Window.Content is FrameworkElement frameworkElement)
        {
            frameworkElement.RequestedTheme = theme;
        }
        SaveUserPreferences("AppTheme", theme);


    }
    public void SetHidingZeroBalances(bool isHiding)
    {
        userPreferences.IsHidingZeroBalances = isHiding;
        SaveUserPreferences("IsHidingZeroBalances", isHiding);
    }
    public void SetCheckingForUpdate(bool isChecking)
    {
        userPreferences.IsCheckForUpdate = isChecking;
        SaveUserPreferences("IsCheckForUpdate", isChecking);
    }
    public void SetExpandingScrollBars(bool isExpanded)
    {
        userPreferences.IsScrollBarsExpanded = isExpanded;
        SaveUserPreferences("IsScrollBarsExpanded", isExpanded);
    }
    public void SetHidingCapitalFlow(bool isHiding)
    {
        userPreferences.IsHidingCapitalFlow = isHiding;
        SaveUserPreferences("IsHidingCapitalFlow", isHiding);
    }

    public NumberFormatInfo GetNumberFormat()
    {
        return userPreferences.NumberFormat;
    }
    public string GetAppCultureLanguage()
    {
        return userPreferences.AppCultureLanguage;
    }
    public ElementTheme GetAppTheme()
    {
        return userPreferences.AppTheme;
    }
    public bool GetHidingZeroBalances()
    {
        return userPreferences.IsHidingZeroBalances;
    }
    public bool GetCheckingForUpdate()
    {
        return userPreferences.IsCheckForUpdate;
    }
    public AppFontSize GetFontSize()
    {
        return userPreferences.FontSize;
    }
    public bool GetExpandingScrollBars()
    {
        return userPreferences.IsScrollBarsExpanded;
    }
    public bool GetHidingCapitalFlow()
    {
        return userPreferences.IsHidingCapitalFlow;
    }
    public int GetRefreshIntervalMinutes()
    {
        return userPreferences.RefreshIntervalMinutes;
    }

    public void LoadUserPreferencesFromXml()
    {
        try
        {
            if (File.Exists(App.appDataPath + "\\prefs.xml"))
            {
                App.isAppInitializing = true;
                var mySerializer = new XmlSerializer(typeof(UserPreferences));
                using var myFileStream = new FileStream(App.appDataPath + "\\prefs.xml", FileMode.Open);

                userPreferences = (mySerializer.Deserialize(myFileStream) as UserPreferences) ?? new UserPreferences();
            }
        }
        catch { }
        finally
        {
            App.isAppInitializing = false;
        }
    }




    public void SaveUserPreferences(string propertyName, object value)
    {
        if (App.isAppInitializing)
        {
            return;
        }

        Logger?.Information("{0} set to {1}", propertyName, value.ToString());
        var mySerializer = new XmlSerializer(typeof(UserPreferences));
        var myWriter = new StreamWriter(App.appDataPath + "\\prefs.xml");
        mySerializer.Serialize(myWriter, this.userPreferences);
        myWriter.Close();
    }

    public void AttachLogger()
    {
        Logger = Log.Logger.ForContext(Constants.SourceContextPropertyName, typeof(PreferencesService).Name.PadRight(22));

        Logger.Information("AppCultureLanguage set to {0}", userPreferences.AppCultureLanguage.ToString());
        Logger.Information("DecimalSeparator set to {0}", userPreferences.NumberFormat.NumberDecimalSeparator.ToString());
        Logger.Information("Font Size set to {0}", userPreferences.FontSize.ToString());
        Logger.Information("IsHidingZeroBalances set to {0}", userPreferences.IsHidingZeroBalances.ToString());
        Logger.Information("Theme set to {0}", userPreferences.AppTheme.ToString());
        Logger.Information("IsScrollBarsExpanded set to {0}", userPreferences.IsScrollBarsExpanded.ToString());
        Logger.Information("IsCheckForUpdate set to {0}", userPreferences.IsCheckForUpdate);
        Logger.Information("IsHidingCapitalFlow set to {0}", userPreferences.IsHidingCapitalFlow);

    }
    private void SetCulture()
    {
        //if (App.Localizer == null)
        //{
        //    return;
        //}

        //App.Localizer.SetLanguage(userPreferences.AppCultureLanguage);
    }

    private static void SetTheme(ElementTheme theme)
    {
        //if (App.Window != null && App.Window.Content is FrameworkElement frameworkElement)
        //{
        //    frameworkElement.RequestedTheme = theme;
        //}
    }





}

