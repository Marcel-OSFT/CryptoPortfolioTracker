using Microsoft.UI.Xaml.Documents;

namespace CryptoPortfolioTracker.Dialogs;

public sealed partial class DescriptionDialog : ContentDialog
{
    private readonly ILocalizer loc = Localizer.Get();
    private readonly CoinLibraryViewModel _viewModel;

    public DescriptionDialog(Coin coin, CoinLibraryViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
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
        if (sender.ActualTheme != _viewModel.AppSettings.AppTheme)
        {
            sender.RequestedTheme = _viewModel.AppSettings.AppTheme;
        }
    }
}
