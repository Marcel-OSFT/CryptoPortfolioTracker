
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

public class PreferencesService
{
    private UserPreferences userPreferences;
    private readonly Settings? _settings;

    public PreferencesService()
    {
        userPreferences = new UserPreferences();
        _settings = App.Container.GetService<Settings>();
    }

    public UserPreferences GetPreferences()
    {
        return userPreferences;
    }

    public void LoadUserPreferencesFromXml()
    {
        try
        {
            if (File.Exists(AppConstants.AppDataPath + "\\prefs.xml"))
            {
                var mySerializer = new XmlSerializer(typeof(UserPreferences));
                using var myFileStream = new FileStream(AppConstants.AppDataPath + "\\prefs.xml", FileMode.Open);

                userPreferences = (mySerializer.Deserialize(myFileStream) as UserPreferences) ?? new UserPreferences();
            }
        }
        catch { }
    }

    public async Task AssignUserPreferencesToSettingsAsync(CancellationToken ct = default)
    {
        if (_settings == null || userPreferences == null)
        {
            return;
        }
        // Basic UI / culture settings
        _settings.AppTheme = userPreferences.AppTheme;
        _settings.AppCultureLanguage = userPreferences.AppCultureLanguage;

        // Identity / intervals
        _settings.UserID = userPreferences.UserID;
        _settings.PriceUpdateIntervalMinutes = userPreferences.RefreshIntervalMinutes;

        // UI behavior flags
        _settings.IsScrollBarsExpanded = userPreferences.IsScrollBarsExpanded;
        _settings.IsHidingZeroBalances = userPreferences.IsHidingZeroBalances;
        _settings.IsHidingCapitalFlow = userPreferences.IsHidingCapitalFlow;
        _settings.IsCheckForUpdate = userPreferences.IsCheckForUpdate;

        // Number formatting and display
        _settings.NumberFormat = userPreferences.NumberFormat;
        _settings.FontSize = userPreferences.FontSize;
        _settings.AreValuesMasked = userPreferences.AreValuesMasked;

        // Chart / analysis settings
        _settings.WithinRangePerc = userPreferences.WithinRangePerc;
        _settings.CloseToPerc = userPreferences.CloseToPerc;
        _settings.RsiPeriod = userPreferences.RsiPeriod;
        _settings.MaPeriod = userPreferences.MaPeriod;
        _settings.MaType = userPreferences.MaType;

        // Misc UI / features
        _settings.MaxPieCoins = userPreferences.MaxPieCoins;
        _settings.HeatMapIndex = userPreferences.HeatMapIndex;

        // Last seen / version
        _settings.LastPortfolio = userPreferences.LastPortfolio;
        _settings.LastVersion = userPreferences.LastVersion;

        // wait for background writes to complete before removing the old prefs.xml
        try
        {
            await _settings.FlushPreferenceStoreAsync(ct).ConfigureAwait(false);
            var xmlPath = Path.Combine(AppConstants.AppDataPath, "prefs.xml");
            if (File.Exists(xmlPath))
            {
                File.Delete(xmlPath);
            }
        }
        catch
        {
            // handle/log if needed; don't throw from migration path unless desired
        }

    }



}

