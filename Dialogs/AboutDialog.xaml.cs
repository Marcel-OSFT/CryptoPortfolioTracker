using CommunityToolkit.Mvvm.ComponentModel;
using CryptoPortfolioTracker.Enums;
using CryptoPortfolioTracker.Services;
using LiveChartsCore.Themes;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using WinUI3Localizer;

namespace CryptoPortfolioTracker.Dialogs;

[ObservableObject]
public sealed partial class AboutDialog : BaseDialog, IDialog<bool>
{
    public bool Result { get; private set; }
    [ObservableProperty] private string imagePath;
    [ObservableProperty] private string btcImage;
    [ObservableProperty] private string ethImage;
    [ObservableProperty] private string usdcImage;
    [ObservableProperty] private string version;
    [ObservableProperty] private string btcAddress;
    [ObservableProperty] private string ethAddress;
    [ObservableProperty] private string usdcArbAddress;
    [ObservableProperty] private string usdcBscAddress;

    public AboutDialog()
    {
        ImagePath = AppConstants.AppPath + "\\Assets\\CryptoPortfolioTracker.ico";
        BtcImage = AppConstants.AppPath + "\\Assets\\bitcoin.png";
        EthImage = AppConstants.AppPath + "\\Assets\\ethereum.png";
        UsdcImage = AppConstants.AppPath + "\\Assets\\usdc.png";
        Version = AppConstants.ProductVersion;
        BtcAddress = string.Empty;
        EthAddress = string.Empty;
        UsdcArbAddress = string.Empty;
        UsdcBscAddress = string.Empty;
        InitializeComponent();
        DataContext = this;
        SetDialogTitleAndButtons();
    }

    private void SetDialogTitleAndButtons()
    {
        Title = loc.GetLocalizedString("AboutDialog_Title");
        PrimaryButtonText = loc.GetLocalizedString("Common_CloseButton");
        IsPrimaryButtonEnabled = true;
    }

    private async void Dialog_Loading(Microsoft.UI.Xaml.FrameworkElement sender, object args)
    {
        await GetCryptoAddresses();
    }

    public async Task GetCryptoAddresses()
    {
        /* Temporary output file to work with (located in AppData)*/
        var temp_file = AppConstants.AppDataPath + "\\addresses.txt";
        var addressesUrl = AppConstants.Url + "//addresses.txt";

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

    private void PrimaryButton_Click(ContentDialog sender, ContentDialogButtonClickEventArgs e)
    {
        Result = true;
    }

}

