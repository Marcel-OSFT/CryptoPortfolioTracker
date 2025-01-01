
using CryptoPortfolioTracker.Enums;
using CryptoPortfolioTracker.Models;
using Microsoft.UI.Xaml;
using System.Collections.Generic;
using System.Globalization;

namespace CryptoPortfolioTracker.Services
{
    public interface IPreferencesService
    {
        string GetAppCultureLanguage();
        bool GetCheckingForUpdate();
        bool GetExpandingScrollBars();
        AppFontSize GetFontSize();
        bool GetHidingZeroBalances();
        NumberFormatInfo GetNumberFormat();
        UserPreferences GetPreferences();
        ElementTheme GetAppTheme();
        void SetAppCultureLanguage(string language);
        void SetAppTheme(ElementTheme theme);
        void SetCheckingForUpdate(bool isChecking);
        void SetExpandingScrollBars(bool isExpanded);
        void SetFontSize(AppFontSize value);
        void SetHidingCapitalFlow(bool isHiding);
        void SetHidingZeroBalances(bool isHiding);
        void SetNumberFormat(NumberFormatInfo nf);
        bool GetHidingCapitalFlow();
        int GetCloseToPerc();
        int GetWithinRangePerc();
        void SetCloseToPerc(int value);
        void SetWithinRangePerc(int value);

        void AttachLogger();
        void LoadUserPreferencesFromXml();
        int GetRefreshIntervalMinutes();
        void SetMaxPieCoins(int maxPieCoins);
        public int GetMaxPieCoins();
        void SetAreValuesMasked(bool value);
        bool GetAreValesMasked();
        void AddTeachingTips(List<TeachingTipCPT> list);
        TeachingTipCPT GetTeachingTip(string name);
        void SetTeachingTipAsShown(string name);
    }
}
