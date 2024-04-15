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
        //Logger = Log.Logger.ForContext<UserPreferences>();
        Logger = Log.Logger.ForContext(Constants.SourceContextPropertyName, typeof(UserPreferences).Name.PadRight(22));
        cultureLanguage = CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator == "," ? "nl" : "en";
        isHidingZeroBalances = false;
        isScrollBarsExpanded = false;
        isCheckForUpdate = true;
        fontSize = AppFontSize.Normal;
        appTheme = ApplicationTheme.Dark;
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

    internal string cultureLanguage;
    public string CultureLanguage
    {
        get => cultureLanguage;
        set
        {
            if (value != cultureLanguage)
            {
                cultureLanguage = value;
                SetCulture();
                SaveUserPreferences("CultureLanguage", value.ToString());
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

    private void SetCulture()
    {
        CultureInfo.CurrentCulture = new CultureInfo(CultureLanguage);
        CultureInfo.DefaultThreadCurrentCulture = new CultureInfo(CultureLanguage); //App.cultureInfoNl;
        CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo(CultureLanguage); //App.cultureInfoNl;
    }

    public void SaveUserPreferences(string propertyName, string value)
    {
        Logger.Information("{0} set to {1}", propertyName, value);
        
        if (App.isReadingUserPreferences) return;
        XmlSerializer mySerializer = new XmlSerializer(typeof(UserPreferences));
        StreamWriter myWriter = new StreamWriter(App.appDataPath + "\\prefs.xml");
        mySerializer.Serialize(myWriter, this);
        myWriter.Close();
    }

    public void AttachLogger()
    {
        Logger = Log.Logger.ForContext(Constants.SourceContextPropertyName, typeof(UserPreferences).Name.PadRight(22));

        Logger.Information("CultureLanguage set to {0}", CultureLanguage.ToString());
        Logger.Information("Font Size set to {0}", FontSize.ToString());
        Logger.Information("IsHidingZeroBalances set to {0}", IsHidingZeroBalances.ToString());
        Logger.Information("Theme set to {0}", AppTheme.ToString());
        Logger.Information("IsScrollBarsExpanded set to {0}", IsScrollBarsExpanded.ToString());
        Logger.Information("IsCheckForUpdate set to {0}", IsCheckForUpdate);

    }
}

