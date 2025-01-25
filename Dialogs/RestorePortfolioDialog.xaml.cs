using CommunityToolkit.Mvvm.Input;
using CryptoPortfolioTracker.Models;
using CryptoPortfolioTracker.Services;
using CryptoPortfolioTracker.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using Windows.Storage.Pickers;
using Windows.Storage;
using WinRT.Interop;
using WinUI3Localizer;
using System;
using CommunityToolkit.Mvvm.ComponentModel;
using System.IO.Compression;
using System.IO;
using System.Linq;

namespace CryptoPortfolioTracker.Dialogs;

[ObservableObject]
public sealed partial class RestorePortfolioDialog :  ContentDialog
{
    public Portfolio? Portfolio;

    [ObservableProperty] private string portfolioName;
    [ObservableProperty] private string fileName;
    [ObservableProperty] private string explanation;


    private readonly IPreferencesService _preferencesService;
    private readonly PortfolioService _portfolioService;
    private readonly ILocalizer loc = Localizer.Get();

    public RestorePortfolioDialog(PortfolioService portfolioService, IPreferencesService preferencesService, AdminViewModel viewModel)
    {
        this.InitializeComponent();
        _preferencesService = preferencesService;
        _portfolioService = portfolioService;
        DataContext = this;
        SetDialogTitleAndButtons();
    }

    [RelayCommand]
    private async void Browse()
    {
        var picker = new FileOpenPicker
        {
            ViewMode = PickerViewMode.Thumbnail,
            SuggestedStartLocation = PickerLocationId.Desktop
        };
        picker.FileTypeFilter.Add(".bak");

        // Get the handle of the current window
        var hwnd = WindowNative.GetWindowHandle(App.Window);
        InitializeWithWindow.Initialize(picker, hwnd);

        StorageFile file = await picker.PickSingleFileAsync();
        if (file != null)
        {
           

            FileName = file.Path;
            var tempFolder = Path.Combine(App.AppDataPath, "Temp");

            // temporary extraction to get the signature
            if (Directory.Exists(tempFolder)) Directory.Delete(tempFolder, true);
            try
            {
                ZipFile.ExtractToDirectory(FileName, tempFolder);
            }
            catch (Exception ex)
            {
                Explanation = loc.GetLocalizedString("RestorePortfolioDialog_ExplError");
                return;
            }

            var signature = Directory.GetDirectories(tempFolder,"*-*-*-*-*").FirstOrDefault();
            
            if (signature == string.Empty)
            {
                Explanation = loc.GetLocalizedString("RestorePortfolioDialog_ExplNoSignature");
            }

            signature = Path.GetRelativePath(tempFolder, signature);

            //check if this backup belongs to an existing portfolio
            var portfolio = _portfolioService.GetPortfolioBySignature(signature);

            if (portfolio != null)
            {
                PortfolioName = portfolio.Name;
                FileName = file.Path;
                // show Expainer text that this backup belongs to an existing portfolio
                Explanation = loc.GetLocalizedString("RestorePortfolioDialog_ExplPortfolioExist");
                txtName.Visibility = Visibility.Visible;
                Portfolio = portfolio;
            }
            else
            {
                // The backup is from a portfolio that is not in the actual list. So provide the oportunity to restore
                // it as a new Portfolio with the name that is stored in the pid.json file
                var pidFile = Directory.GetFiles(Path.Combine(tempFolder, signature), "*pid.json").FirstOrDefault();
                var pid = PortfolioService.GetPidFromJson(pidFile);

                PortfolioName = pid;
                FileName = file.Path;
                // show Explainer text that this backup is from a portfolio that is not in the actual list. 
                Explanation = loc.GetLocalizedString("RestorePortfolioDialog_ExplPortfolioNotExist");
                txtName.Visibility = Visibility.Visible;
                Portfolio = new Portfolio() {Name = pid, Signature = signature };

            }
            if (Directory.Exists(tempFolder)) Directory.Delete(tempFolder, true);
            IsPrimaryButtonEnabled = true;
        }
        else
        {
            // No file was selected
            Portfolio = null;
        }
    }


    private void SetDialogTitleAndButtons()
    {
        Title = loc.GetLocalizedString("PortfolioDialog_Title_Restore");
        PrimaryButtonText = loc.GetLocalizedString("PortfolioDialog_PrimaryButton_Restore");
        CloseButtonText = loc.GetLocalizedString("PortfolioDialog_CloseButton");
        IsPrimaryButtonEnabled = false;
        Explanation = string.Empty;
        txtName.Visibility = Visibility.Collapsed;
    }

    private void Dialog_Loading(Microsoft.UI.Xaml.FrameworkElement sender, object args)
    {
        if (sender.ActualTheme != _preferencesService.GetAppTheme())
        {
            sender.RequestedTheme = _preferencesService.GetAppTheme();
        }
    }

    private void Button_Click_Accept(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {

    }
}
