using System;
using System.Globalization;
using CryptoPortfolioTracker.Models;
using Microsoft.UI.Dispatching;

namespace CryptoPortfolioTracker.Configuration;

public partial class Settings : ObservableObject
{
    private readonly IPreferenceStore _store;

    public Settings(IPreferenceStore store)
    {
        _store = store;
    }

    public ElementTheme AppTheme
    {
        get => _store.Get("AppTheme", ElementTheme.Default);
        set
        {
            _store.Set("AppTheme", value);
            OnPropertyChanged(nameof(AppTheme));
        }
    }

    public string AppCultureLanguage
    {
        get => _store.Get("AppCultureLanguage", CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLower() == "nl" ? "nl" : "en-US");
        set
        {
            _store.Set("AppCultureLanguage", value.ToLower());
            OnPropertyChanged(nameof(AppCultureLanguage));

            if (App.Localizer == null)
            {
                return;
            }

            App.Localizer.SetLanguage(value.ToLower());
        }
    }

    public string UserID
    {
        get => _store.Get("UserId", Guid.NewGuid().ToString());
        set
        {
            _store.Set("UserId", value);
            OnPropertyChanged(nameof(UserID));
        }
    }

    public int PriceUpdateIntervalMinutes
    {
        get => _store.Get("PriceUpdateIntervalMinutes", 2);
        set
        {
            _store.Set("PriceUpdateIntervalMinutes", value);
            OnPropertyChanged(nameof(PriceUpdateIntervalMinutes));
        }
    }

    public bool IsScrollBarsExpanded
    {
        get => _store.Get("IsScrollBarsExpanded", false);
        set
        {
            _store.Set("IsScrollBarsExpanded", value);
            OnPropertyChanged(nameof(IsScrollBarsExpanded));
        }
    }

    public bool IsHidingZeroBalances
    {
        get => _store.Get("IsHidingZeroBalances", false);
        set
        {
            _store.Set("IsHidingZeroBalances", value);
            OnPropertyChanged(nameof(IsHidingZeroBalances));
        }
    }

    public NumberFormatInfo NumberFormat
    {
        get
        {
            var decimalSeparator = _store.Get("NumberFormat - Decimal Separator", CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalSeparator);
            var groupSeparator = _store.Get("NumberFormat - Group Separator", CultureInfo.CurrentUICulture.NumberFormat.NumberGroupSeparator);
            var nf = (NumberFormatInfo)CultureInfo.CurrentUICulture.NumberFormat.Clone();
            nf.NumberDecimalSeparator = decimalSeparator;
            nf.NumberGroupSeparator = groupSeparator;
            return nf;
        }
        set
        {
            _store.Set("NumberFormat - Decimal Separator", value.NumberDecimalSeparator);
            _store.Set("NumberFormat - Group Separator", value.NumberGroupSeparator);
            OnPropertyChanged(nameof(NumberFormat));
        }
    }

    public bool IsCheckForUpdate
    {
        get => _store.Get("IsCheckForUpdate", true);
        set
        {
            _store.Set("IsCheckForUpdate", value);
            OnPropertyChanged(nameof(IsCheckForUpdate));
        }
    }

    public AppFontSize FontSize
    {
        get => _store.Get("FontSize", AppFontSize.Normal);
        set
        {
            _store.Set("FontSize", value);
            OnPropertyChanged(nameof(FontSize));
        }
    }

    public bool IsHidingCapitalFlow
    {
        get => _store.Get("IsHidingCapitalFlow", false);
        set
        {
            _store.Set("IsHidingCapitalFlow", value);
            OnPropertyChanged(nameof(IsHidingCapitalFlow));
        }
    }

    public int WithinRangePerc
    {
        get => _store.Get("WithinRangePerc", 5);
        set
        {
            _store.Set("WithinRangePerc", value);
            OnPropertyChanged(nameof(WithinRangePerc));
        }
    }

    public int CloseToPerc
    {
        get => _store.Get("CloseToPerc", 5);
        set
        {
            _store.Set("CloseToPerc", value);
            OnPropertyChanged(nameof(CloseToPerc));
        }
    }

    public int RsiPeriod
    {
        get => _store.Get("RsiPeriod", 14);
        set
        {
            _store.Set("RsiPeriod", value);
            OnPropertyChanged(nameof(RsiPeriod));
        }
    }

    public int MaPeriod
    {
        get => _store.Get("MaPeriod", 50);
        set
        {
            _store.Set("MaPeriod", value);
            OnPropertyChanged(nameof(MaPeriod));
        }
    }

    public string MaType
    {
        get => _store.Get("MaType", "SMA");
        set
        {
            _store.Set("MaType", value);
            OnPropertyChanged(nameof(MaType));
        }
    }

    public int MaxPieCoins
    {
        get => _store.Get("MaxPieCoins", 10);
        set
        {
            _store.Set("MaxPieCoins", value);
            OnPropertyChanged(nameof(MaxPieCoins));
        }
    }

    public bool AreValuesMasked
    {
        get => _store.Get("AreValuesMasked", false);
        set
        {
            _store.Set("AreValuesMasked", value);
            OnPropertyChanged(nameof(AreValuesMasked));
        }
    }

    public int HeatMapIndex
    {
        get => _store.Get("HeatMapIndex", 0);
        set
        {
            _store.Set("HeatMapIndex", value);
            OnPropertyChanged(nameof(HeatMapIndex));
        }
    }

    public Portfolio? LastPortfolio
    {
        get => _store.Get<Portfolio?>("LastPortfolio", null);
        set
        {
            _store.Set("LastPortfolio", value);
            OnPropertyChanged(nameof(LastPortfolio));
        }
    }

    public string LastVersion
    {
        get => _store.Get("LastVersion", "0.0.0");
        set
        {
            _store.Set("LastVersion", value);
            OnPropertyChanged(nameof(LastVersion));
        }
    }
    // New: expose flush so callers owning Settings can wait for persistence
    public Task FlushPreferenceStoreAsync(CancellationToken ct = default) =>
        _store.FlushAsync(ct);
}