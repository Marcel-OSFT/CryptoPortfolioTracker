namespace CryptoPortfolioTracker.Dialogs;

public sealed partial class CopyPortfolioDialog : ContentDialog
{
    private Portfolio? _portfolioToCopy;
    public string NewPortfolioName { get; private set; }
    public bool NeedPartialCopy { get; private set; }

    public readonly AdminViewModel _viewModel;

    private readonly ILocalizer loc = Localizer.Get();

    public CopyPortfolioDialog(AdminViewModel viewModel, Portfolio portfolio)
    {
        this.InitializeComponent();
        _viewModel = viewModel;
        _portfolioToCopy = portfolio;
        SetDialogTitleAndButtons();
    }


    private void SetDialogTitleAndButtons()
    {
        txtName.Text = _portfolioToCopy is not null ? _portfolioToCopy.Name + " - Copy" : string.Empty;
            
        Title = loc.GetLocalizedString("PortfolioDialog_Title_Copy") + $" {_portfolioToCopy.Name}";
        PrimaryButtonText = loc.GetLocalizedString("PortfolioDialog_PrimaryButton_Copy");
        CloseButtonText = loc.GetLocalizedString("PortfolioDialog_CloseButton");
        IsPrimaryButtonEnabled = txtName.Text.Length > 0;
    }


    private void Button_Click_Accept(ContentDialog sender, ContentDialogButtonClickEventArgs e)
    {
        NewPortfolioName = MkOsft.NormalizeName(txtName.Text);
    }

    private void PortfolioName_TextChanging(TextBox sender, TextBoxTextChangingEventArgs args)
    {
        IsPrimaryButtonEnabled = txtName.Text.Length > 0;
    }


    private void Dialog_Loading(Microsoft.UI.Xaml.FrameworkElement sender, object args)
    {
        if (sender.ActualTheme != _viewModel.AppSettings.AppTheme)
        {
            sender.RequestedTheme = _viewModel.AppSettings.AppTheme;
        }
    }

    private void PortfolioName_LosingFocus(Microsoft.UI.Xaml.UIElement sender, Microsoft.UI.Xaml.Input.LosingFocusEventArgs args)
    {
        var doesExist = _viewModel._portfolioService.DoesPortfolioNameExist(txtName.Text);

        txtName.Text = MkOsft.NormalizeName(txtName.Text);
        
        if (doesExist)
        {
            txtName.Text = string.Empty;
            txtName.PlaceholderText = loc.GetLocalizedString("PortfolioDialog_NameExists");
        }

    }

    private void CopyOptions_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is RadioButtons rb && rb.SelectedIndex >= 0)
        {
            if (rb.SelectedIndex == 0)
            {
                NeedPartialCopy = false;
            }
            else
            {
                NeedPartialCopy = true;
            }
        }
    }
}
