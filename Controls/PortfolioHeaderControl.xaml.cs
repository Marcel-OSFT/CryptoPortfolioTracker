using Microsoft.UI.Xaml;
using System.Windows.Input;

namespace CryptoPortfolioTracker.Controls;

public sealed partial class PortfolioHeaderControl : UserControl
{
    public PortfolioHeaderControl()
    {
        InitializeComponent();
        UpdateVisibility();
    }

    // Portfolio name
    public string PortfolioName
    {
        get => (string)GetValue(PortfolioNameProperty);
        set => SetValue(PortfolioNameProperty, value);
    }

    public static readonly DependencyProperty PortfolioNameProperty =
        DependencyProperty.Register(
            nameof(PortfolioName),
            typeof(string),
            typeof(PortfolioHeaderControl),
            new PropertyMetadata(string.Empty));

    // Current view title
    public string ViewTitle
    {
        get => (string)GetValue(ViewTitleProperty);
        set => SetValue(ViewTitleProperty, value);
    }

    public static readonly DependencyProperty ViewTitleProperty =
        DependencyProperty.Register(
            nameof(ViewTitle),
            typeof(string),
            typeof(PortfolioHeaderControl),
            new PropertyMetadata(string.Empty));

    // Optional item count
    public string ItemCountText
    {
        get => (string)GetValue(ItemCountTextProperty);
        set => SetValue(ItemCountTextProperty, value);
    }

    public static readonly DependencyProperty ItemCountTextProperty =
        DependencyProperty.Register(
            nameof(ItemCountText),
            typeof(string),
            typeof(PortfolioHeaderControl),
            new PropertyMetadata(string.Empty));

    // Explanatory text
    public string ExplainerText
    {
        get => (string)GetValue(ExplainerTextProperty);
        set => SetValue(ExplainerTextProperty, value);
    }

    public static readonly DependencyProperty ExplainerTextProperty =
        DependencyProperty.Register(
            nameof(ExplainerText),
            typeof(string),
            typeof(PortfolioHeaderControl),
            new PropertyMetadata(string.Empty, OnOptionalContentChanged));

    // Privacy glyph
    public string PrivacyGlyph
    {
        get => (string)GetValue(PrivacyGlyphProperty);
        set => SetValue(PrivacyGlyphProperty, value);
    }

    public static readonly DependencyProperty PrivacyGlyphProperty =
        DependencyProperty.Register(
            nameof(PrivacyGlyph),
            typeof(string),
            typeof(PortfolioHeaderControl),
            new PropertyMetadata(string.Empty));

    // Privacy command
    public ICommand PrivacyCommand
    {
        get => (ICommand)GetValue(PrivacyCommandProperty);
        set => SetValue(PrivacyCommandProperty, value);
    }

    public static readonly DependencyProperty PrivacyCommandProperty =
        DependencyProperty.Register(
            nameof(PrivacyCommand),
            typeof(ICommand),
            typeof(PortfolioHeaderControl),
            new PropertyMetadata(null, OnOptionalContentChanged));

    // Settings command
    public ICommand SettingsCommand
    {
        get => (ICommand)GetValue(SettingsCommandProperty);
        set => SetValue(SettingsCommandProperty, value);
    }

    public static readonly DependencyProperty SettingsCommandProperty =
        DependencyProperty.Register(
            nameof(SettingsCommand),
            typeof(ICommand),
            typeof(PortfolioHeaderControl),
            new PropertyMetadata(null, OnOptionalContentChanged));

    // Optional portfolio totals
    public bool ShowPortfolioTotals
    {
        get => (bool)GetValue(ShowPortfolioTotalsProperty);
        set => SetValue(ShowPortfolioTotalsProperty, value);
    }

    public static readonly DependencyProperty ShowPortfolioTotalsProperty =
        DependencyProperty.Register(
            nameof(ShowPortfolioTotals),
            typeof(bool),
            typeof(PortfolioHeaderControl),
            new PropertyMetadata(false, OnOptionalContentChanged));

    public string TotalPortfolioValue
    {
        get => (string)GetValue(TotalPortfolioValueProperty);
        set => SetValue(TotalPortfolioValueProperty, value);
    }

    public static readonly DependencyProperty TotalPortfolioValueProperty =
        DependencyProperty.Register(
            nameof(TotalPortfolioValue),
            typeof(string),
            typeof(PortfolioHeaderControl),
            new PropertyMetadata(string.Empty));

    public string ProfitLossPercentage
    {
        get => (string)GetValue(ProfitLossPercentageProperty);
        set => SetValue(ProfitLossPercentageProperty, value);
    }

    public static readonly DependencyProperty ProfitLossPercentageProperty =
        DependencyProperty.Register(
            nameof(ProfitLossPercentage),
            typeof(string),
            typeof(PortfolioHeaderControl),
            new PropertyMetadata(string.Empty));

    private static void OnOptionalContentChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e)
    {
        if (d is PortfolioHeaderControl control)
        {
            control.UpdateVisibility();
        }
    }

    private void UpdateVisibility()
    {
        if (PrivacyButton == null)
            return;

        PrivacyButton.Visibility =
            PrivacyCommand != null
                ? Visibility.Visible
                : Visibility.Collapsed;

        SettingsButton.Visibility =
            SettingsCommand != null
                ? Visibility.Visible
                : Visibility.Collapsed;

        InfoButton.Visibility =
            !string.IsNullOrWhiteSpace(ExplainerText)
                ? Visibility.Visible
                : Visibility.Collapsed;

        PortfolioTotalsPanel.Visibility =
            ShowPortfolioTotals
                ? Visibility.Visible
                : Visibility.Collapsed;
    }
}

