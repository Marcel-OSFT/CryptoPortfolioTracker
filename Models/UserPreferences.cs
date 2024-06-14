using System;
using System.Globalization;
using CryptoPortfolioTracker.Enums;
using Microsoft.UI.Xaml;

namespace CryptoPortfolioTracker.Models;

[Serializable]
public class UserPreferences
{
    public UserPreferences()
    {
        SetDefaultPreferences();
    }

    private void SetDefaultPreferences()
    {
        isHidingZeroBalances = false;
        isScrollBarsExpanded = false;
        isCheckForUpdate = true;
        fontSize = AppFontSize.Normal;
        appTheme = ElementTheme.Default;
        refreshIntervalMinutes = 5;
        isHidingCapitalFlow = false;

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
                //SaveUserPreferences(nameof(RefreshIntervalMinutes), value);
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
                //SetTheme(value);
                //SaveUserPreferences(nameof(AppTheme), value);
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
                //SaveUserPreferences(nameof(IsScrollBarsExpanded), value);
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
                //SaveUserPreferences(nameof(IsHidingZeroBalances), value);
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
                //SaveUserPreferences(nameof(NumberFormat), value);
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
                //SetCulture();
                //SaveUserPreferences(nameof(AppCultureLanguage), value);
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
                //SaveUserPreferences(nameof(IsCheckForUpdate), value);
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
                //SaveUserPreferences(nameof(FontSize), value);
            }
        }
    }

    private bool isHidingCapitalFlow;
    public bool IsHidingCapitalFlow
    {
        get => isHidingCapitalFlow;
        set
        {
            if (value != isHidingCapitalFlow)
            {
                isHidingCapitalFlow = value;
                //SaveUserPreferences(nameof(IsHidingCapitalFlow), value);
            }
        }
    }


}

