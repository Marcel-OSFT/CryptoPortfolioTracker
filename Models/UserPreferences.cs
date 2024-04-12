using CryptoPortfolioTracker.Enums;
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

namespace CryptoPortfolioTracker.Models
{
    [Serializable]
    public class UserPreferences
    {

        public UserPreferences() 
        {
            cultureLanguage = CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator == "," ? "nl" : "en";
            isHidingZeroBalances = false;
            isScrollBarsExpanded = false;
            isCheckForUpdate = true;
            fontSize = AppFontSize.Normal;
        }


        private bool isScrollBarsExpanded;
        public bool IsScrollBarsExpanded
        {
            get { return isScrollBarsExpanded; }
            set
            {
                if (value != isScrollBarsExpanded)
                {
                    isScrollBarsExpanded = value;
                    SaveUserPreferences();  
                }
            }
        }


        private bool isHidingZeroBalances;
        public bool IsHidingZeroBalances
        {
            get { return isHidingZeroBalances; }
            set
            {
                if (value != isHidingZeroBalances)
                {
                    isHidingZeroBalances = value;
                    SaveUserPreferences();
                }
            }
        }

        internal string cultureLanguage;
        public string CultureLanguage
        {
            get { return cultureLanguage; }
            set
            {
                if (value != cultureLanguage)
                {
                    cultureLanguage = value;
                    SetCulture();
                    SaveUserPreferences();
                }
            }
        }

        private bool isCheckForUpdate;
        public bool IsCheckForUpdate
        {
            get { return isCheckForUpdate; }
            set
            {
                if (value != isCheckForUpdate)
                {
                    isCheckForUpdate = value;
                    SaveUserPreferences();
                }
            }
        }


        private AppFontSize fontSize;
        public AppFontSize FontSize
        {
            get { return fontSize; }
            set
            {
                if (value != fontSize)
                {
                    fontSize = value;
                    SaveUserPreferences();
                }
            }
        }


        private void SetCulture()
        {
            CultureInfo.CurrentCulture = new CultureInfo(CultureLanguage);
            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo(CultureLanguage); //App.cultureInfoNl;
            CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo(CultureLanguage); //App.cultureInfoNl;

        }


        public void SaveUserPreferences()
        {
            if (App.isReadingUserPreferences) return;
            XmlSerializer mySerializer = new XmlSerializer(typeof(UserPreferences));
            StreamWriter myWriter = new StreamWriter(App.appDataPath + "\\prefs.xml");
            mySerializer.Serialize(myWriter, this);
            myWriter.Close();
        }

       

    }
    
}
