namespace CryptoPortfolioTracker.Dialogs;

public sealed partial class AccountDialog : BaseDialog, IDialog<Account>
{
    public Account? Result { get; private set; }
    public readonly AccountsViewModel _viewModel;
    public Account? newAccount;
    private readonly Account? _accountToEdit;

    private readonly DialogAction _dialogAction;

    public AccountDialog(AccountsViewModel viewModel, AccountDialogParameters parameters)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _accountToEdit = parameters.account;
        SetDialogTitleAndButtons(parameters.dialogAction);
        _dialogAction = parameters.dialogAction;
        
    }

    private void SetDialogTitleAndButtons(DialogAction dialogAction)
    {
        if (dialogAction == DialogAction.Edit)
        {
            txtAccountName.Text = _accountToEdit is not null ? _accountToEdit.Name : string.Empty;
            DescriptionText.Text = _accountToEdit is not null ? _accountToEdit.About : string.Empty;
            Title = loc.GetLocalizedString("AccountDialog_Title_Edit");
            PrimaryButtonText = loc.GetLocalizedString("AccountDialog_PrimaryButton_Edit");
            CloseButtonText = loc.GetLocalizedString("AccountDialog_CloseButton");
            IsPrimaryButtonEnabled = txtAccountName.Text.Length > 0;
        }
        else
        {
            Title = loc.GetLocalizedString("AccountDialog_Title_Add");
            PrimaryButtonText = loc.GetLocalizedString("AccountDialog_PrimaryButton_Add");
            CloseButtonText = loc.GetLocalizedString("AccountDialog_CloseButton");
            IsPrimaryButtonEnabled = txtAccountName.Text.Length > 0;
        }
    }

    private void Button_Click_AcceptAccount(ContentDialog sender, ContentDialogButtonClickEventArgs e)
    {
        var account = new Account
        {
            Name = txtAccountName.Text,
            About = DescriptionText.Text,
        };
        newAccount = account;
        Result = account;
    }

    private void AccountName_TextChanging(TextBox sender, TextBoxTextChangingEventArgs args)
    {
        IsPrimaryButtonEnabled = txtAccountName.Text.Length > 0;
    }
    

    private void AccountName_LosingFocus(Microsoft.UI.Xaml.UIElement sender, Microsoft.UI.Xaml.Input.LosingFocusEventArgs args)
    {
        var doesExist = _viewModel._accountService.DoesAccountNameExist(txtAccountName.Text);

        if (doesExist && _dialogAction == DialogAction.Edit)
        {
            if (_accountToEdit?.Name.ToLower() != txtAccountName.Text.ToLower())
            {
                txtAccountName.Text = string.Empty;
                txtAccountName.PlaceholderText = loc.GetLocalizedString("AccountDialog_AccountNameExists");
            }
            
        }
        else if(doesExist && _dialogAction == DialogAction.Add)
        {
            txtAccountName.Text = string.Empty;
            txtAccountName.PlaceholderText = loc.GetLocalizedString("AccountDialog_AccountNameExists");
        }



        

    }

}

