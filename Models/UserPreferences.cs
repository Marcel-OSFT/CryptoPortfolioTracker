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
