using CryptoPortfolioTracker.Models;
using CryptoPortfolioTracker.Services;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using WinUI3Localizer;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CryptoPortfolioTracker.Dialogs;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class DescriptionDialog : ContentDialog
{
    private readonly ILocalizer loc = Localizer.Get();
    private readonly IPreferencesService _preferencesService;
    public DescriptionDialog(Coin coin, IPreferencesService preferencesService)
    {
        InitializeComponent();
        _preferencesService = preferencesService;
        var run = new Run
        {
            //TODO Pull Coin Description from the Web API in a localized way 

            // currently the 'About' is stored in the database and never refreshed
            // we could pull it from the Web Api in a localized way
            Text = coin.About
        };
        par.Inlines.Clear();
        par.Inlines.Add(run);
        SetDialogTitleAndButtons(coin);
    }

    private void SetDialogTitleAndButtons(Coin coin)
    {
        Title = loc.GetLocalizedString("DescriptionDialog_Title") + " " + coin.Name; ;
        CloseButtonText = loc.GetLocalizedString("DescriptionDialog_CloseButton");
        IsPrimaryButtonEnabled = false;
    }

    private void Dialog_Loading(Microsoft.UI.Xaml.FrameworkElement sender, object args)
    {
        if (sender.ActualTheme != _preferencesService.GetAppTheme())
        {
            sender.RequestedTheme = _preferencesService.GetAppTheme();
        }
    }
}
