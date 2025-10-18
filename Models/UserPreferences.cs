using System;
using System.Collections.Generic;
using System.Globalization;
using CryptoPortfolioTracker.Enums;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

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

        closeToPerc = 5;
        withinRangePerc = 20;
        MaxPieCoins = 10;
        AreValuesMasked = false;
        HeatMapIndex = 0;

        LastPortfolio = null;
        RsiPeriod = 14;
        MaPeriod = 50;
        MaType = "SMA";

    }
    public string? PasswordHash { get; set; }
    public string? DuressPasswordHash { get; set; }

    private int refreshIntervalMinutes;
    public int RefreshIntervalMinutes
    {
        get => refreshIntervalMinutes;
        set
        {
            if (value != refreshIntervalMinutes)
            {
                refreshIntervalMinutes = value;
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
            }
        }
    }

    private string userID = string.Empty;
    public string UserID
    {
        get => userID;
        set
        {
            if (value != userID)
            {
                userID = value;
            }
        }
    }

    private string lastVersion = string.Empty;
    public string LastVersion
    {
        get => lastVersion;
        set
        {
            if (value != lastVersion)
            {
                lastVersion = value;
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
            }
        }
    }

    private NumberFormatInfo numberFormat = new();
    public NumberFormatInfo NumberFormat
    {
        get => numberFormat;
        set
        {
            if (value != numberFormat)
            {
                numberFormat = value;
            }
        }
    }

    private string appCultureLanguage = string.Empty;
    public string AppCultureLanguage
    {
        get => appCultureLanguage;
        set
        {
            if (value.ToLower() != appCultureLanguage.ToLower())
            {
                appCultureLanguage = value.ToLower();
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
            }
        }
    }

    private int withinRangePerc;
    public int WithinRangePerc
    {
        get => withinRangePerc;
        set
        {
            if (value != withinRangePerc)
            {
                withinRangePerc = value;

            }
        }
    }
    private int rsiPeriod;
    public int RsiPeriod
    {
        get => rsiPeriod;
        set
        {
            if (value != rsiPeriod)
            {
                rsiPeriod = value;

            }
        }
    }
    private int maPeriod;
    public int MaPeriod
    {
        get => maPeriod;
        set
        {
            if (value != maPeriod)
            {
                maPeriod = value;

            }
        }
    }private string maType;
    public string MaType
    {
        get => maType;
        set
        {
            if (value != maType)
            {
                maType = value;

            }
        }
    }

    private int closeToPerc;
    public int CloseToPerc
    {
        get => closeToPerc;
        set
        {
            if (value != closeToPerc)
            {
                closeToPerc = value;

            }
        }
    }

    //create property for MaxPieCoins
    private int maxPieCoins;
    public int MaxPieCoins
    {
        get => maxPieCoins;
        set
        {
            if (value != maxPieCoins)
            {
                maxPieCoins = value;
            }
        }
    }

    private bool areValuesMasked;
    public bool AreValuesMasked
    {
        get => areValuesMasked;
        set
        {
            if (value != areValuesMasked)
            {
                areValuesMasked = value;
            }
        }
    }

    public Portfolio? LastPortfolio { get; set; }

    private int heatMapIndex;
    public int HeatMapIndex
    {
        get => heatMapIndex;
        set
        {
            if (value != heatMapIndex)
            {
                heatMapIndex = value;
            }
        }
    }



}

