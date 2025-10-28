using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CryptoPortfolioTracker.Enums;
using Microsoft.UI.Xaml.Controls;
using static System.Net.WebRequestMethods;
using File = System.IO.File;

namespace CryptoPortfolioTracker.Models;

public class AppUpdater
{
    private string downloadLink = string.Empty;
    private string downloadsFolderPath = string.Empty;
    private string fileName = string.Empty;
    private string latestVersion = string.Empty;

    /// 2. Declare DownloadsFolder GUI and import SHGetKnownFolderPath method
    private static Guid FolderDownloads = new("374DE290-123F-4565-9164-39C4925E467B");
    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    private static extern int SHGetKnownFolderPath(ref Guid id, int flags, IntPtr token, out IntPtr path);

    public AppUpdater()
    {
    }

    public async Task<AppUpdaterResult> Check(string updateUrl, string appVersion)
    {
        /* Temporary output file to work with (located in AppData)*/
        var temp_version_file = AppConstants.AppDataPath + "\\current_version.txt";

        /* Use the WebClient class to download the file from your server */
        using (var httpClient = new HttpClient())
        {
            try
            {
                var response = await httpClient.GetAsync(updateUrl);

                if (!response.IsSuccessStatusCode)
                {
                    // write an error
                    return AppUpdaterResult.CheckingError;
                }

                using var fs = new FileStream(temp_version_file, FileMode.Create);
                await response.Content.CopyToAsync(fs);
            }
            catch (Exception)
            {
                /* Handle exceptions */
                return AppUpdaterResult.CheckingError;
            }
        }

        /* Check if temporary file was downloaded of not */
        if (File.Exists(temp_version_file))
        {
            /* Get the file content and split it in two */
            //var version_data = File.ReadAllText(temp_version_file).Split('=');
            var version_data = File.ReadAllText(temp_version_file).Split(';');


            /* Variable to store the app new version (without the periods)*/
            latestVersion = version_data[0];
            int _latestVersion = Convert.ToInt16(string.Concat(latestVersion.Split('.')));
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

    public static string GetDownloadsPath()
    {
        if (Environment.OSVersion.Version.Major < 6)
        {
            throw new NotSupportedException();
        }

        var pathPtr = IntPtr.Zero;

        try
        {
            _ = SHGetKnownFolderPath(ref FolderDownloads, 0, IntPtr.Zero, out pathPtr);
            return Marshal.PtrToStringUni(pathPtr) + "\\";
        }
        finally
        {
            Marshal.FreeCoTaskMem(pathPtr);
        }
    }

    private string ExtractFileName()
    {
        if (downloadLink == null || downloadLink == string.Empty)
        {
            return string.Empty;
        }

        string[] sections;
        try
        {
            sections = downloadLink.Split("/");
        }
        catch
        {
            return string.Empty;
        }
        return sections.Length > 0 ? sections[^1] : string.Empty;
    }

    public async Task<AppUpdaterResult> DownloadSetupFile()
    {
        downloadsFolderPath = GetDownloadsPath();
        //fileName = ExtractFileName();
        fileName = "CryptoPortfolioTracker_setup_" + latestVersion + ".exe";

        using var httpClient = new HttpClient();
        HttpResponseMessage response;
        httpClient.Timeout = TimeSpan.FromSeconds(1000);
        try
        {
            response = await httpClient.GetAsync(downloadLink);
            if (!response.IsSuccessStatusCode)
            {
                // write an error
                return AppUpdaterResult.DownloadError;
            }
            using var fs = new FileStream(downloadsFolderPath + fileName, FileMode.Create);
            await response.Content.CopyToAsync(fs);
        }
        catch (Exception ex)
        {
            /* Handle exceptions */
            return AppUpdaterResult.DownloadError;
        }
        return AppUpdaterResult.DownloadSuccesfull;
    }

    public void ExecuteSetupFile()
    {
        try
        {
            Process process = new();
            process.StartInfo.FileName = downloadsFolderPath + fileName;
            process.StartInfo.Arguments = "/SP- /silent /noicons /forcecloseapplications /restartapplications";
            process.StartInfo.WorkingDirectory = AppConstants.AppPath;
            process.Start();
        }
        catch (Exception)
        {
        }
        //After starting setup.exe, exit your application as soon as possible.Note that to avoid problems with updating your.exe, Setup has an auto retry feature.
        //Optionally you could also use the skipifsilent and skipifnotsilent flags and make your application aware of a '/updated' parameter to for example show a nice message box to inform the user that the update has completed.
        Environment.Exit(0);
    }


}
