using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
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
            
            CultureLanguage = CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator == "," ? "nl" : "en";
            IsHidingZeroBalances = false;
            
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


        internal string _CultureLanguage;
        public string CultureLanguage
        {
            get { return _CultureLanguage; }
            set
            {
                if (value != _CultureLanguage)
                {
                    _CultureLanguage = value;
                    SetCulture();
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
            StreamWriter myWriter = new StreamWriter(App.appPath + "\\prefs.xml");
            mySerializer.Serialize(myWriter, this);
            myWriter.Close();
        }


    }
}
