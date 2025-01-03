using CryptoPortfolioTracker.Enums;
using CryptoPortfolioTracker.Models;
using CryptoPortfolioTracker.Services;
using CryptoPortfolioTracker.ViewModels;
using Microsoft.UI.Xaml.Controls;
using WinUI3Localizer;

namespace CryptoPortfolioTracker.Dialogs;

public sealed partial class NarrativeDialog : ContentDialog
{
    public readonly NarrativesViewModel _viewModel;
    public Narrative? newNarrative;
    private readonly Narrative? _narrativeToEdit;
    private readonly IPreferencesService _preferencesService;

    private readonly ILocalizer loc = Localizer.Get();
    private readonly DialogAction _dialogAction;

    public NarrativeDialog(IPreferencesService preferencesService, NarrativesViewModel viewModel, DialogAction dialogAction = DialogAction.Add, Narrative? NarrativeToEdit = null)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _narrativeToEdit = NarrativeToEdit;
        _preferencesService = preferencesService;
        SetDialogTitleAndButtons(dialogAction);
        _dialogAction = dialogAction;
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
    }

    private void NarrativeName_TextChanging(TextBox sender, TextBoxTextChangingEventArgs args)
    {
        IsPrimaryButtonEnabled = txtNarrativeName.Text.Length > 0;
    }
    

    private void Dialog_Loading(Microsoft.UI.Xaml.FrameworkElement sender, object args)
    {
        if (sender.ActualTheme != _preferencesService.GetAppTheme())
        {
            sender.RequestedTheme = _preferencesService.GetAppTheme();
        }
    }

    private void NarrativeName_LosingFocus(Microsoft.UI.Xaml.UIElement sender, Microsoft.UI.Xaml.Input.LosingFocusEventArgs args)
    {
        var doesExist = _viewModel._narrativeService.DoesNarrativeNameExist(txtNarrativeName.Text);

        if (doesExist && _dialogAction == DialogAction.Edit)
        {
            if (_narrativeToEdit?.Name.ToLower() != txtNarrativeName.Text.ToLower())
            {
                txtNarrativeName.Text = string.Empty;
                txtNarrativeName.PlaceholderText = loc.GetLocalizedString("AccountDialog_AccountNameExists");
            }

        }
        else if (doesExist && _dialogAction == DialogAction.Add)
        {
            txtNarrativeName.Text = string.Empty;
            txtNarrativeName.PlaceholderText = loc.GetLocalizedString("AccountDialog_AccountNameExists");
        }

    }
}

