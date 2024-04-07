using CommunityToolkit.Common;
using CryptoPortfolioTracker.Enums;
using CryptoPortfolioTracker.Views;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Windows.Storage;

namespace CryptoPortfolioTracker.Models
{
    public class AppUpdater
    {
        private string downloadLink = null;

        /// 2. Declare DownloadsFolder GUI and import SHGetKnownFolderPath method
        private static Guid FolderDownloads = new Guid("374DE290-123F-4565-9164-39C4925E467B");
        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern int SHGetKnownFolderPath(ref Guid id, int flags, IntPtr token, out IntPtr path);




        public AppUpdater() { }

        public async Task<AppUpdaterResult> Check(string updateUrl, string appVersion)
        {
            /* Temporary output file to work with (located in AppData)*/
            var temp_version_file = App.appDataPath + "\\current_version.txt";

            /* Use the WebClient class to download the file from your server */
            using (var httpClient = new HttpClient())
            {
                try
                {
                    var response = await httpClient.GetAsync(updateUrl);

                    if (!response.IsSuccessStatusCode)
                    {
                        // write an error
                        return AppUpdaterResult.Error;
                    }

                    using (var fs = new FileStream(temp_version_file, FileMode.Create))
                    {
                        await response.Content.CopyToAsync(fs);
                    }
                }
                catch (Exception)
                {
                    /* Handle exceptions */
                    return AppUpdaterResult.Error;
                }
            }

            /* Check if temporary file was downloaded of not */
            if (File.Exists(temp_version_file))
            {
                /* Get the file content and split it in two */
                string[] version_data = File.ReadAllText(temp_version_file).Split('=');

                /* Variable to store the app new version (without the periods)*/
                int _latestVersion = Convert.ToInt16(string.Concat(version_data[0].Split('.')));
                int _appVersion = Convert.ToInt16(string.Concat(appVersion.Split(".")));

                /* Store the download link in the global variable already created */
                downloadLink = version_data[1];

                /* Compare the app current version with the version from the downloaded file */
                if (_latestVersion > _appVersion)
                {
                    return AppUpdaterResult.NeedUpdate;
                }
            }

            /* Delete the temporary file after using it */
            if (File.Exists(temp_version_file))
            {
                File.Delete(temp_version_file);
            }
            return AppUpdaterResult.UpToDate;
        }

        /// <summary>
        /// Returns the absolute downloads directory specified on the system.
        /// </summary>
        /// <returns></returns>
        public static string GetDownloadsPath()
        {
            if (Environment.OSVersion.Version.Major < 6) throw new NotSupportedException();

            IntPtr pathPtr = IntPtr.Zero;

            try
            {
                SHGetKnownFolderPath(ref FolderDownloads, 0, IntPtr.Zero, out pathPtr);
                return Marshal.PtrToStringUni(pathPtr) + "\\";
            }
            finally
            {
                Marshal.FreeCoTaskMem(pathPtr);
            }
        }

        private string ExtractFileName()
        {
            if (downloadLink == null) return string.Empty;
            string[] sections;
            try
            {
                sections = downloadLink.Split("/");
            }
            catch
            {
                return string.Empty;
            }
            return  sections.Length > 0 ? sections[sections.Length-1] : string.Empty;
        }

        public async Task<AppUpdaterResult> Update()
        {
            //-- TO DO implement downloading and installing

            downloadLink = "https://marcel-osft.github.io/CryptoPortfolioTracker/CryptoPortfolioTracker_setup.exe";


            var DownloadsFolderPath = GetDownloadsPath();
            var fileName = ExtractFileName();

            //Download the new installer
            using (var httpClient = new HttpClient())
            {
                HttpResponseMessage response;
                try
                {
                    response = await httpClient.GetAsync(downloadLink);

                    if (!response.IsSuccessStatusCode)
                    {
                        // write an error
                        return AppUpdaterResult.Error;
                    }

                    using (var fs = new FileStream(DownloadsFolderPath + fileName, FileMode.Create))
                    {
                        await response.Content.CopyToAsync(fs);
                    }
                }
                catch (Exception)
                {
                    /* Handle exceptions */
                    return AppUpdaterResult.Error;
                }

            }
            return AppUpdaterResult.UpdateSuccesfull;

            /* Perform the update operation, maybe just download
            *  the new version with any installed browser or 
            *  implement a download function with progressbar 
            *  and the like, that's your choice.
            *
            *  Example:
            */
            //*** then run process.start to execute the installer...

            // Process.Start(download_link); /SP- /silent /noicons "/dir=expand:{commonpf64}\{#MyAppName}"

            //After starting setup.exe, exit your application as soon as possible.Note that to avoid problems with updating your.exe, Setup has an auto retry feature.
            //Optionally you could also use the skipifsilent and skipifnotsilent flags and make your application aware of a '/updated' parameter to for example show a nice message box to inform the user that the update has completed.

        }



    }
}
