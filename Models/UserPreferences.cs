using CryptoPortfolioTracker.Enums;
using Microsoft.UI.Xaml;
using Serilog;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace CryptoPortfolioTracker.Models;

[Serializable]
public class UserPreferences
{
    private Serilog.ILogger Logger { get; set; }
    public UserPreferences() 
    {
        numberFormat = new NumberFormatInfo(); 
        numberFormat = CultureInfo.CurrentCulture.NumberFormat;
        isHidingZeroBalances = false;
        isScrollBarsExpanded = false;
        isCheckForUpdate = true;
        fontSize = AppFontSize.Normal;
        appTheme = ApplicationTheme.Dark;
        refreshIntervalMinutes = 5;
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
                SaveUserPreferences("RefreshIntervalMinutes", value.ToString());
            }
        }
    }

    private ApplicationTheme appTheme;
    public ApplicationTheme AppTheme
    {
        get => appTheme;
        set
        {
            if (value != appTheme)
            {
                appTheme = value;
                SaveUserPreferences("AppTheme", value.ToString());
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

                SaveUserPreferences("IsScrollBarsExtended", value.ToString());
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
                SaveUserPreferences("IsHidingZeroBalances", value.ToString());
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
                SaveUserPreferences("NumberFormat", value.ToString());
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
                SaveUserPreferences("IsCheckForUpdate", value.ToString());
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
                SaveUserPreferences("FontSize", value.ToString());
            }
        }
    }

    public void SaveUserPreferences(string propertyName, string value)
    {
        if (App.isReadingUserPreferences) return;
        
        Logger.Information("{0} set to {1}", propertyName, value);
        XmlSerializer mySerializer = new XmlSerializer(typeof(UserPreferences));
        StreamWriter myWriter = new StreamWriter(App.appDataPath + "\\prefs.xml");
        mySerializer.Serialize(myWriter, this);
        myWriter.Close();
    }

    public void AttachLogger()
    {
        Logger = Log.Logger.ForContext(Constants.SourceContextPropertyName, typeof(UserPreferences).Name.PadRight(22));

        Logger.Information("DecimalSeparator set to {0}", NumberFormat.CurrencyDecimalSeparator.ToString());
        Logger.Information("Font Size set to {0}", FontSize.ToString());
        Logger.Information("IsHidingZeroBalances set to {0}", IsHidingZeroBalances.ToString());
        Logger.Information("Theme set to {0}", AppTheme.ToString());
        Logger.Information("IsScrollBarsExpanded set to {0}", IsScrollBarsExpanded.ToString());
        Logger.Information("IsCheckForUpdate set to {0}", IsCheckForUpdate);

    }
}

