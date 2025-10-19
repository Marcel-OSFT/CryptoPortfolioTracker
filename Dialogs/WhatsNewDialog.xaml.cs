using CommunityToolkit.Mvvm.ComponentModel;
using CryptoPortfolioTracker.Services;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Net.Http;
using System.Runtime.InteropServices;
using Windows.Storage;
using Windows.Storage.Streams;

namespace CryptoPortfolioTracker.Dialogs;

[ObservableObject]
public sealed partial class WhatsNewDialog : ContentDialog
{
    [ComImport, System.Runtime.InteropServices.Guid("3E68D4BD-7135-4D10-8018-9FB6D9F33FA1"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IInitializeWithWindow
    {
        void Initialize([In] IntPtr hwnd);
    }

    [DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Auto, PreserveSig = true, SetLastError = false)]
    public static extern IntPtr GetActiveWindow();



    private const string RtfUrl_NL = "https://marcel-osft.github.io/CryptoPortfolioTracker/docs/WhatsNew_NL.rtf";
    private const string RtfUrl_EN = "https://marcel-osft.github.io/CryptoPortfolioTracker/docs/WhatsNew_EN.rtf";
    private readonly IPreferencesService _preferenceService;
    private static readonly System.Net.Http.HttpClient _httpClient = new HttpClient();

    [ObservableProperty] private StorageFile? rtfDoc;

    public WhatsNewDialog(IPreferencesService preferencesService)
    {
        InitializeComponent();
        _preferenceService = preferencesService;
    }

    //private async void WhatsNewDialog_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    //{
    //    try
    //    {
    //        RichText.IsReadOnly = false;
    //        StorageFile helpFile = await StorageFile.GetFileFromPathAsync(App.AppPath + "\\WhatsNew_EN.rtf");
    //        Windows.Storage.Streams.IRandomAccessStream randAccStream = await helpFile.OpenAsync(Windows.Storage.FileAccessMode.Read);

    //        // Load the file into the Document property of the RichEditBox.
    //        RichText.Document.LoadFromStream(Microsoft.UI.Text.TextSetOptions.FormatRtf, randAccStream);
    //        RichText.IsReadOnly = true;
    //    }
    //    catch (Exception)
    //    {

    //    }

    //}
    private async void WhatsNewDialog_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        try
        {
            RichText.IsReadOnly = false;

            // Choose URL based on app culture language preference
            string appLang = _preferenceService?.GetAppCultureLanguage() ?? "en";
            string rtfUrl = appLang.StartsWith("nl", StringComparison.OrdinalIgnoreCase) ? RtfUrl_NL : RtfUrl_EN;

            // Download RTF bytes
            byte[] rtfBytes = await _httpClient.GetByteArrayAsync(rtfUrl);

            // Write bytes into an in-memory random access stream and load into RichEditBox
            var randAccStream = new InMemoryRandomAccessStream();
            var dataWriter = new DataWriter(randAccStream.GetOutputStreamAt(0));
            dataWriter.WriteBytes(rtfBytes);
            await dataWriter.StoreAsync();
            await dataWriter.FlushAsync();
            dataWriter.DetachStream();
            dataWriter.Dispose();

            randAccStream.Seek(0);

            RichText.Document.LoadFromStream(Microsoft.UI.Text.TextSetOptions.FormatRtf, randAccStream);
            RichText.IsReadOnly = true;
        }
        catch (Exception)
        {
            // Keep silent on failure to avoid breaking the dialog flow.
            // Optionally add logging here.
        }

    }

}