
using CommunityToolkit.Mvvm.Messaging;
using CryptoPortfolioTracker.Enums;
using CryptoPortfolioTracker.Models;
using Microsoft.UI.Xaml;
using Serilog;
using Serilog.Core;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Xml.Serialization;
using Windows.Security.Credentials;


namespace CryptoPortfolioTracker.Services;

public class PreferencesService : IPreferencesService
{
    private UserPreferences userPreferences;
    private readonly IMessenger _messenger;
    private Serilog.ILogger Logger { get; set; }
    public PreferencesService(IMessenger messenger)
    {
        userPreferences = new UserPreferences();
        Logger = Log.Logger.ForContext(Constants.SourceContextPropertyName, typeof(PreferencesService).Name.PadRight(22));
        _messenger = messenger;
    }

    public UserPreferences GetPreferences()
    {
        return userPreferences;
    }

   

    public void SetNumberFormat(NumberFormatInfo nf)
    {
        userPreferences.NumberFormat = nf;
        SaveUserPreferences("NumberFormat - Decimal Separator", nf.NumberDecimalSeparator);
    }
    public void SetHeatMapIndex(int index)
    {
        userPreferences.HeatMapIndex = index;
        SaveUserPreferences("HeatMapIndex", index);
    }
    public void SetRsiPeriod(int value)
    {
        userPreferences.RsiPeriod = value;
        SaveUserPreferences("RsiPeriod", value);
    }
    public void SetMaPeriod(int value)
    {
        userPreferences.MaPeriod = value;
        SaveUserPreferences("MaPeriod", value);
    }
    public void SetMaType(string value)
    {
        userPreferences.MaType = value;
        SaveUserPreferences("MaType", value);
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
    public void SetCloseToPerc(int closeToPerc)
    {
        userPreferences.CloseToPerc = closeToPerc;
        SaveUserPreferences("CloseToPerc", closeToPerc);
    }
    public void SetWithinRangePerc(int withinRangePerc)
    {
        userPreferences.WithinRangePerc = withinRangePerc;
        SaveUserPreferences("WithinRangePerc", withinRangePerc);
    }

    public void SetMaxPieCoins(int maxPieCoins)
    {
        userPreferences.MaxPieCoins = maxPieCoins;
        SaveUserPreferences("MaxPieCoins", maxPieCoins);
    }
    public void SetAreValuesMasked(bool value)
    {
        userPreferences.AreValuesMasked = value;
        SaveUserPreferences("AreValuesMasked", value);
    }
    public void SetLastVersion(string value)
    {
        userPreferences.LastVersion = value;
        SaveUserPreferences("LastVersion", value);
    }
    public void SetUserID(string value)
    {
        userPreferences.UserID = value;
        SaveUserPreferences("UserID", value);
    }
    public void SetLastPortfolio(Portfolio? value)
    {
        if (value == null)
        {
            return;
        }
        userPreferences.LastPortfolio = value;
        SaveUserPreferences("LastPortfolio", value);
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
    public int GetHeatMapIndex()
    {
        return userPreferences.HeatMapIndex;
    }

    public int GetCloseToPerc()
    {
        return userPreferences.CloseToPerc;
    }
    public int GetWithinRangePerc()
    {
        return userPreferences.WithinRangePerc;
    }

    public int GetMaxPieCoins()
    {
        return userPreferences.MaxPieCoins;
    }

    public bool GetAreValesMasked()
    {
        return userPreferences.AreValuesMasked;
    }
    public string GetLastVersion()
    {
        return userPreferences.LastVersion;
    }
    public string GetUserID()
    {
        return userPreferences.UserID;
    }
    public int GetRsiPeriod()
    {
        return userPreferences.RsiPeriod;
    }
    public int GetMaPeriod()
    {
        return userPreferences.MaPeriod;
    }
    public string GetMaType()
    {
        return userPreferences.MaType;
    }


    public void LoadUserPreferencesFromXml()
    {
        try
        {
            if (File.Exists(App.AppDataPath + "\\prefs.xml"))
            {
                var mySerializer = new XmlSerializer(typeof(UserPreferences));
                using var myFileStream = new FileStream(App.AppDataPath + "\\prefs.xml", FileMode.Open);

                userPreferences = (mySerializer.Deserialize(myFileStream) as UserPreferences) ?? new UserPreferences();
            }
        }
        catch { }
    }

    public void SaveUserPreferences(string propertyName, object value)
    {
        Logger?.Information("{0} set to {1}", propertyName, value.ToString());
        var mySerializer = new XmlSerializer(typeof(UserPreferences));
        var myWriter = new StreamWriter(App.AppDataPath + "\\prefs.xml");
        mySerializer.Serialize(myWriter, this.userPreferences);
        myWriter.Close();
        _messenger.Send(new PreferencesChangedMessage());
    }

    public void AttachLogger()
    {
        Logger.Information("AppCultureLanguage set to {0}", userPreferences.AppCultureLanguage.ToString());
        Logger.Information("DecimalSeparator set to {0}", userPreferences.NumberFormat.NumberDecimalSeparator.ToString());
        Logger.Information("Font Size set to {0}", userPreferences.FontSize.ToString());
        Logger.Information("IsHidingZeroBalances set to {0}", userPreferences.IsHidingZeroBalances.ToString());
        Logger.Information("Theme set to {0}", userPreferences.AppTheme.ToString());
        Logger.Information("IsScrollBarsExpanded set to {0}", userPreferences.IsScrollBarsExpanded.ToString());
        Logger.Information("IsCheckForUpdate set to {0}", userPreferences.IsCheckForUpdate);
        Logger.Information("IsHidingCapitalFlow set to {0}", userPreferences.IsHidingCapitalFlow);
    }

    public Portfolio GetLastPortfolio()
    {
        return userPreferences.LastPortfolio;
    }

}

