namespace CryptoPortfolioTracker.Dialogs;

public sealed partial class PortfolioDialog : BaseDialog, IDialog<Portfolio>
{
    public Portfolio Result { get; private set; }
    private Portfolio? _portfolioToEdit;
    public Portfolio? newPortfolio;

    public readonly AdminViewModel _viewModel;

    private readonly DialogAction _dialogAction;

    public PortfolioDialog(AdminViewModel viewModel, PortfolioDialogParameters parameters)
    {
        this.InitializeComponent();
        _viewModel = viewModel;
        _portfolioToEdit = parameters.portfolio;
        SetDialogTitleAndButtons(parameters.dialogAction);
        _dialogAction = parameters.dialogAction;
    }


    private void SetDialogTitleAndButtons(DialogAction dialogAction)
    {
        if (dialogAction == DialogAction.Edit)
        {
            txtName.Text = _portfolioToEdit is not null ? _portfolioToEdit.Name : string.Empty;
            
            Title = loc.GetLocalizedString("PortfolioDialog_Title_Edit") + $" {_portfolioToEdit.Name}";
            PrimaryButtonText = loc.GetLocalizedString("PortfolioDialog_PrimaryButton_Edit");
            CloseButtonText = loc.GetLocalizedString("PortfolioDialog_CloseButton");
            IsPrimaryButtonEnabled = txtName.Text.Length > 0;
        }
        else
        {
            Title = loc.GetLocalizedString("PortfolioDialog_Title_Add");
            PrimaryButtonText = loc.GetLocalizedString("PortfolioDialog_PrimaryButton_Add");
            CloseButtonText = loc.GetLocalizedString("PortfolioDialog_CloseButton");
            IsPrimaryButtonEnabled = txtName.Text.Length > 0;
        }
    }


    private void Button_Click_AcceptAccount(ContentDialog sender, ContentDialogButtonClickEventArgs e)
    {
        var portfolio = new Portfolio()
        {
            Name = MkOsft.NormalizeName(txtName.Text),
            
        };
        newPortfolio = portfolio;
        Result = portfolio;
    }

    private void PortfolioName_TextChanging(TextBox sender, TextBoxTextChangingEventArgs args)
    {
        IsPrimaryButtonEnabled = txtName.Text.Length > 0;
    }

    private void PortfolioName_LosingFocus(Microsoft.UI.Xaml.UIElement sender, Microsoft.UI.Xaml.Input.LosingFocusEventArgs args)
    {
        var doesExist = _viewModel._portfolioService.DoesPortfolioNameExist(txtName.Text);

        txtName.Text = MkOsft.NormalizeName(txtName.Text);


        if (doesExist && _dialogAction == DialogAction.Edit)
        {
            if (_portfolioToEdit?.Name.ToLower() != txtName.Text.ToLower())
            {
                txtName.Text = string.Empty;
                txtName.PlaceholderText = loc.GetLocalizedString("PortfolioDialog_NameExists");
            }

        }
        else if (doesExist && _dialogAction == DialogAction.Add)
        {
            txtName.Text = string.Empty;
            txtName.PlaceholderText = loc.GetLocalizedString("PortfolioDialog_NameExists");
        }

    }

}
