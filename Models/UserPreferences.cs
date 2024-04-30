﻿using System;
using System.Globalization;
using System.IO;
using System.Xml.Serialization;
using CryptoPortfolioTracker.Enums;
using Microsoft.UI.Xaml;
using Serilog;
using Serilog.Core;

namespace CryptoPortfolioTracker.Models;

[Serializable]
public class UserPreferences
{
    private Serilog.ILogger Logger
    {
        get; set;
    }
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public UserPreferences()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    {
        isHidingZeroBalances = false;
        isScrollBarsExpanded = false;
        isCheckForUpdate = true;
        fontSize = AppFontSize.Normal;
        appTheme = ElementTheme.Default;
        refreshIntervalMinutes = 5;

        if (CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLower() == "nl")
        {
            appCultureLanguage = "nl";
        }
        else
        {
            appCultureLanguage = "en-US";
        }
        numberFormat = new NumberFormatInfo();
        numberFormat = CultureInfo.CurrentUICulture.NumberFormat;
    }

    private int refreshIntervalMinutes;
    public int RefreshIntervalMinutes
    {
        get => refreshIntervalMinutes;
        set
        {
            if (value != refreshIntervalMinutes)
            {
                refreshIntervalMinutes = value;
                SaveUserPreferences(nameof(RefreshIntervalMinutes), value);
            }
        }
    }

    private ElementTheme appTheme;
    public ElementTheme AppTheme
    {
        get => appTheme;
        set
        {
            if (value != appTheme)
            {
                appTheme = value;
                SetTheme(value);
                SaveUserPreferences(nameof(AppTheme), value);
            }
        }
    }


    private bool isScrollBarsExpanded;
    public bool IsScrollBarsExpanded
    {
        get => isScrollBarsExpanded;
        set
        {
            if (value != isScrollBarsExpanded)
            {
                isScrollBarsExpanded = value;
                SaveUserPreferences(nameof(IsScrollBarsExpanded), value);
            }
        }
    }


    private bool isHidingZeroBalances;
    public bool IsHidingZeroBalances
    {
        get => isHidingZeroBalances;
        set
        {
            if (value != isHidingZeroBalances)
            {
                isHidingZeroBalances = value;
                SaveUserPreferences(nameof(IsHidingZeroBalances), value);
            }
        }
    }

    private NumberFormatInfo numberFormat;
    public NumberFormatInfo NumberFormat
    {
        get => numberFormat;
        set
        {
            if (value != numberFormat)
            {
                numberFormat = value;
                SaveUserPreferences(nameof(NumberFormat), value);
            }
        }
    }

    private string appCultureLanguage;
    public string AppCultureLanguage
    {
        get => appCultureLanguage;
        set
        {
            if (value != appCultureLanguage)
            {
                appCultureLanguage = value;
                SetCulture();
                SaveUserPreferences(nameof(AppCultureLanguage), value);
            }
        }
    }



    private bool isCheckForUpdate;
    public bool IsCheckForUpdate
    {
        get => isCheckForUpdate;
        set
        {
            if (value != isCheckForUpdate)
            {
                isCheckForUpdate = value;
                SaveUserPreferences(nameof(IsCheckForUpdate), value);
            }
        }
    }


    private AppFontSize fontSize;
    public AppFontSize FontSize
    {
        get => fontSize;
        set
        {
            if (value != fontSize)
            {
                fontSize = value;
                SaveUserPreferences(nameof(FontSize), value);
            }
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
        mySerializer.Serialize(myWriter, this);
        myWriter.Close();
    }

    public void AttachLogger()
    {
        Logger = Log.Logger.ForContext(Constants.SourceContextPropertyName, typeof(UserPreferences).Name.PadRight(22));

        Logger.Information("AppCultureLanguage set to {0}", AppCultureLanguage.ToString());
        Logger.Information("DecimalSeparator set to {0}", NumberFormat.CurrencyDecimalSeparator.ToString());
        Logger.Information("Font Size set to {0}", FontSize.ToString());
        Logger.Information("IsHidingZeroBalances set to {0}", IsHidingZeroBalances.ToString());
        Logger.Information("Theme set to {0}", AppTheme.ToString());
        Logger.Information("IsScrollBarsExpanded set to {0}", IsScrollBarsExpanded.ToString());
        Logger.Information("IsCheckForUpdate set to {0}", IsCheckForUpdate);

    }
    private void SetCulture()
    {
        if (App.Localizer == null)
        {
            return;
        }

        App.Localizer.SetLanguage(AppCultureLanguage);
    }

    private static void SetTheme(ElementTheme theme)
    {
        if (App.Window != null && App.Window.Content is FrameworkElement frameworkElement)
        {
            frameworkElement.RequestedTheme = theme;
        }
    }


}
