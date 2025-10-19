using CommunityToolkit.Mvvm.ComponentModel;
using CryptoPortfolioTracker.Services;
using Microsoft.UI.Xaml.Controls;
using System;

namespace CryptoPortfolioTracker.Dialogs;

[ObservableObject]
public sealed partial class WhatsNewDialog : ContentDialog
{
    private const string PdfUrl_NL = "https://marcel-osft.github.io/CryptoPortfolioTracker/docs/WhatsNew_NL.pdf";
    private const string PdfUrl_EN = "https://marcel-osft.github.io/CryptoPortfolioTracker/docs/WhatsNew_EN.pdf";
    private readonly IPreferencesService _preferenceService;

    [ObservableProperty] private Uri pdfDoc;

    public WhatsNewDialog(IPreferencesService preferencesService)
    {
        InitializeComponent();
        _preferenceService = preferencesService;
        //pdfDoc = new Uri(_preferenceService.GetAppCultureLanguage() == "nl" ? PdfUrl_NL : PdfUrl_EN);
    }

    private async void WhatsNewDialog_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        string userLanguage = _preferenceService.GetAppCultureLanguage();
        await WebView.EnsureCoreWebView2Async();
        WebView.Source = new Uri(userLanguage == "nl" ? PdfUrl_NL : PdfUrl_EN);
    }
}
