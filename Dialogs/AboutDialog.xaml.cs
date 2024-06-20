using CommunityToolkit.Mvvm.ComponentModel;
using CryptoPortfolioTracker.Enums;
using CryptoPortfolioTracker.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.IO;
using System.Threading.Tasks;
using System;
using WinUI3Localizer;
using System.Net.Http;
using System.Diagnostics;

namespace CryptoPortfolioTracker.Dialogs;

[ObservableObject]
public partial class AboutDialog : ContentDialog
{
    private readonly ILocalizer loc = Localizer.Get();
    private readonly ElementTheme _theme;

    [ObservableProperty] private string imagePath;
    [ObservableProperty] private string btcImage;
    [ObservableProperty] private string ethImage;
    [ObservableProperty] private string usdcImage;
    [ObservableProperty] private string version;
    [ObservableProperty] private string btcAddress;
    [ObservableProperty] private string ethAddress;
    [ObservableProperty] private string usdcArbAddress;
    [ObservableProperty] private string usdcBscAddress;

    public AboutDialog(ElementTheme theme)
    {
        ImagePath = App.appPath + "\\Assets\\CryptoPortfolioTracker.ico";
        BtcImage = App.appPath + "\\Assets\\bitcoin.png";
        EthImage = App.appPath + "\\Assets\\ethereum.png";
        UsdcImage = App.appPath + "\\Assets\\usdc.png";
        Version = App.ProductVersion;
        InitializeComponent();
        DataContext = this;
        _theme = theme;
        SetDialogTitleAndButtons();
    }

    private async void Dialog_Loading(Microsoft.UI.Xaml.FrameworkElement sender, object args)
    {
        if (sender.ActualTheme != _theme)
        {
            sender.RequestedTheme = _theme;
        }
        await GetCryptoAddresses();
    }

    private void SetDialogTitleAndButtons()
    {
        Title = loc.GetLocalizedString("AboutDialog_Title");
        PrimaryButtonText = loc.GetLocalizedString("Common_CloseButton");
        IsPrimaryButtonEnabled = true;
    }


    public async Task GetCryptoAddresses()
    {
        /* Temporary output file to work with (located in AppData)*/
        var temp_file = App.appDataPath + "\\addresses.txt";
        var addressesUrl = App.Url + "//addresses.txt";

        /* Use the WebClient class to download the file from your server */
        using (var httpClient = new HttpClient())
        {
            try
            {
                var response = await httpClient.GetAsync(addressesUrl);

                using var fs = new FileStream(temp_file, FileMode.Create);
                await response.Content.CopyToAsync(fs);
            }
            catch (Exception ex)
            {
                /* Handle exceptions */
                Debug.WriteLine(ex.Message);
                return;
            }
        }

        /* Check if temporary file was downloaded of not */
        if (File.Exists(temp_file))
        {
            /* Get the file content and split it in two */
            var address_data = File.ReadAllText(temp_file).Split(';');

            /* Variable to store the app new version (without the periods)*/
            BtcAddress = address_data[0];
            EthAddress = address_data[1];
            UsdcBscAddress = address_data[2];
            //UsdcBscAddress = address_data[3];
        }
        /* Delete the temporary file after using it */
        if (File.Exists(temp_file))
        {
            File.Delete(temp_file);
        }
    }

}
