using Microsoft.UI.Xaml.Documents;

namespace CryptoPortfolioTracker.Dialogs;

public sealed partial class DescriptionDialog : BaseDialog, IDialog<bool>
{
    public bool Result { get; private set; }
    private readonly CoinLibraryViewModel _viewModel;

    public DescriptionDialog(CoinLibraryViewModel viewModel, DescriptionDialogParameters parameters)
    {
        _viewModel = viewModel;
        InitializeComponent();
        var run = new Run
        {
            Text = parameters.coin.About
        };
        par.Inlines.Clear();
        par.Inlines.Add(run);
        SetDialogTitleAndButtons(parameters.coin);
    }

    private void SetDialogTitleAndButtons(Coin coin)
    {
        Title = loc.GetLocalizedString("DescriptionDialog_Title") + " " + coin.Name; ;
        CloseButtonText = loc.GetLocalizedString("DescriptionDialog_CloseButton");
        IsPrimaryButtonEnabled = false;
        Result = true;
    }

}
