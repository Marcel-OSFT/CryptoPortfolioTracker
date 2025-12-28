using System;
using System.Globalization;
using TemperatureMonitor.Models;
using Microsoft.UI.Dispatching;
using CommunityToolkit.Mvvm.Messaging;

namespace TemperatureMonitor.Configuration;

public partial class Settings : ObservableObject
{
    private readonly IPreferenceStore _store;
    private readonly IMessenger _messenger;

    public Settings(IPreferenceStore store, IMessenger messenger)
    {
        _store = store;
        _messenger = messenger;
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

    public AppFontSize FontSize
    {
        get => _store.Get("FontSize", AppFontSize.Normal);
        set
        {
            _store.Set("FontSize", value);
            OnPropertyChanged(nameof(FontSize));
        }
    }

    public int SampleIntervalFastSeconds
    {
        get => _store.Get("SampleIntervalFastSeconds", 10);
        set
        {
            _store.Set("SampleIntervalFastSeconds", value);
            OnPropertyChanged(nameof(SampleIntervalFastSeconds));
            // synchronize ESP and UpdateService with new setting
            MainPage.Current.DispatcherQueue.TryEnqueue(() =>
            {
                _messenger.Send(new SampleRateSettingChangedMessage(DaySelectorMode.Nu));
            });
        }
    }
    public int SampleIntervalSlowMinutes
    {
        get => _store.Get("SampleIntervalSlowMinutes", 10);
        set
        {
            _store.Set("SampleIntervalSlowMinutes", value);
            OnPropertyChanged(nameof(SampleIntervalSlowMinutes));
            // synchronize ESP and UpdateService with new setting
            MainPage.Current.DispatcherQueue.TryEnqueue(() =>
            {
                _messenger.Send(new SampleRateSettingChangedMessage(DaySelectorMode.Dag));
            });
        }
    }

    // New: manual ESP IP address (persisted)
    public string EspIpAddress
    {
        get => _store.Get("EspIpAddress", string.Empty);
        set
        {
            _store.Set("EspIpAddress", value ?? string.Empty);
            OnPropertyChanged(nameof(EspIpAddress));
        }
    }



    // New: expose flush so callers owning Settings can wait for persistence
    public Task FlushPreferenceStoreAsync(CancellationToken ct = default) =>
        _store.FlushAsync(ct);
}