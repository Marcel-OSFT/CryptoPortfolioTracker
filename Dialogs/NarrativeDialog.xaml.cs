namespace CryptoPortfolioTracker.Dialogs;

public sealed partial class NarrativeDialog : BaseDialog, IDialog<Narrative>
{
    public Narrative Result { get; private set;}
    public readonly NarrativesViewModel _viewModel;
    public Narrative? newNarrative;
    private readonly Narrative? _narrativeToEdit;

    private readonly DialogAction _dialogAction;

    public NarrativeDialog(NarrativesViewModel viewModel, NarrativeDialogParameters parameters)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _narrativeToEdit = parameters.narrative;
        SetDialogTitleAndButtons(parameters.dialogAction);
        _dialogAction = parameters.dialogAction;
    }

    private void SetDialogTitleAndButtons(DialogAction dialogAction)
    {
        if (dialogAction == DialogAction.Edit)
        {
            txtNarrativeName.Text = _narrativeToEdit is not null ? _narrativeToEdit.Name : string.Empty;
            DescriptionText.Text = _narrativeToEdit is not null ? _narrativeToEdit.About : string.Empty;
            Title = loc.GetLocalizedString("NarrativeDialog_Title_Edit");
            PrimaryButtonText = loc.GetLocalizedString("NarrativeDialog_PrimaryButton_Edit");
            CloseButtonText = loc.GetLocalizedString("NarrativeDialog_CloseButton");
            IsPrimaryButtonEnabled = txtNarrativeName.Text.Length > 0;
        }
        else
        {
            Title = loc.GetLocalizedString("NarrativeDialog_Title_Add");
            PrimaryButtonText = loc.GetLocalizedString("NarrativeDialog_PrimaryButton_Add");
            CloseButtonText = loc.GetLocalizedString("NarrativeDialog_CloseButton");
            IsPrimaryButtonEnabled = txtNarrativeName.Text.Length > 0;
        }
    }

    private void Button_Click_AcceptNarrative(ContentDialog sender, ContentDialogButtonClickEventArgs e)
    {
        var Narrative = new Narrative
        {
            Name = txtNarrativeName.Text,
            About = DescriptionText.Text,
        };
        newNarrative = Narrative;
        Result = Narrative;
    }

    private void NarrativeName_TextChanging(TextBox sender, TextBoxTextChangingEventArgs args)
    {
        IsPrimaryButtonEnabled = txtNarrativeName.Text.Length > 0;
    }

    private void NarrativeName_LosingFocus(Microsoft.UI.Xaml.UIElement sender, Microsoft.UI.Xaml.Input.LosingFocusEventArgs args)
    {
        var doesExist = _viewModel._narrativeService.DoesNarrativeNameExist(txtNarrativeName.Text);

        bool notAllowed = txtNarrativeName.Text.Contains("stablecoin", StringComparison.OrdinalIgnoreCase);

        if (notAllowed)
        {
            txtNarrativeName.Text = string.Empty;
            txtNarrativeName.PlaceholderText = loc.GetLocalizedString("NarrativeDialog_NarrativeNotAllowed");
        }
        else if (doesExist && _dialogAction == DialogAction.Edit)
        {
            if (_narrativeToEdit?.Name.ToLower() != txtNarrativeName.Text.ToLower())
            {
                txtNarrativeName.Text = string.Empty;
                txtNarrativeName.PlaceholderText = loc.GetLocalizedString("NarrativeDialog_NarrativeNameExists");
            }

        }
        else if (doesExist && _dialogAction == DialogAction.Add)
        {
            txtNarrativeName.Text = string.Empty;
            txtNarrativeName.PlaceholderText = loc.GetLocalizedString("NarrativeDialog_NarrativeNameExists");
        }

    }
}

