using CommunityToolkit.Mvvm.ComponentModel;
using CryptoPortfolioTracker.Services;
using LanguageExt;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

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

    [ObservableProperty] private StorageFile? rtfDoc;

    public WhatsNewDialog(IPreferencesService preferencesService)
    {
        InitializeComponent();
        _preferenceService = preferencesService;
    }

    private async void WhatsNewDialog_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        try
        {
            RichText.IsReadOnly = false;
            StorageFile helpFile = await StorageFile.GetFileFromPathAsync(App.AppPath + "\\HelpFile.rtf");
            Windows.Storage.Streams.IRandomAccessStream randAccStream = await helpFile.OpenAsync(Windows.Storage.FileAccessMode.Read);

            // Load the file into the Document property of the RichEditBox.
            RichText.Document.LoadFromStream(Microsoft.UI.Text.TextSetOptions.FormatRtf, randAccStream);
            RichText.IsReadOnly = true;
        }
        catch (Exception)
        {

        }

    }


    //private async void WhatsNewDialog_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    //{
    //    string userLanguage = _preferenceService.GetAppCultureLanguage();
    //    var docUri = new Uri(userLanguage == "nl" ? RtfUrl_NL : RtfUrl_EN);

    //    try
    //    {
    //        string rtfContent;

    //        if (docUri.Scheme is "http" or "https")
    //        {
    //            using var http = new HttpClient();
    //            var data = await http.GetByteArrayAsync(docUri).ConfigureAwait(false);

    //            // Try to detect common encodings for RTF content:
    //            if (data.Length >= 3 && data[0] == 0xEF && data[1] == 0xBB && data[2] == 0xBF)
    //            {
    //                // UTF-8 with BOM
    //                rtfContent = Encoding.UTF8.GetString(data, 3, data.Length - 3);
    //            }
    //            else
    //            {
    //                rtfContent = TryDecodeUtf8(data) ?? Encoding.GetEncoding(1252).GetString(data);
    //            }

    //            // Persist to a temporary StorageFile so rtfDoc is a StorageFile as requested
    //            var tempFolder = ApplicationData.Current.TemporaryFolder;
    //            var tempFile = await tempFolder.CreateFileAsync($"WhatsNew_{userLanguage}.rtf", CreationCollisionOption.ReplaceExisting);
    //            await FileIO.WriteBytesAsync(tempFile, data);
    //            rtfDoc = tempFile;
    //        }
    //        else
    //        {
    //            // Try to load from application package or local file URI
    //            try
    //            {
    //                var storageFile = await StorageFile.GetFileFromApplicationUriAsync(docUri);
    //                rtfDoc = storageFile;

    //                // Read text (FileIO.ReadTextAsync will use UTF-8 by default for text files)
    //                rtfContent = await FileIO.ReadTextAsync(storageFile);
    //            }
    //            catch
    //            {
    //                // Last-resort: try to treat URI as path and get via GetFileFromPathAsync
    //                var storageFile = await StorageFile.GetFileFromPathAsync(docUri.LocalPath);
    //                rtfDoc = storageFile;
    //                rtfContent = await FileIO.ReadTextAsync(storageFile);
    //            }
    //        }

    //        // Load as RTF into the RichEditBox document on the UI thread
    //        await RunOnUiThreadAsync(() =>
    //        {
    //           RichText.Document.SetText(Microsoft.UI.Text.TextSetOptions.FormatRtf, rtfContent);
    //        }).ConfigureAwait(false);
    //    }
    //    catch (Exception ex)
    //    {
    //        // Ensure UI update happens on UI thread
    //        await RunOnUiThreadAsync(() =>
    //        {
    //            RichText.Document.SetText(Microsoft.UI.Text.TextSetOptions.None, $"Failed to load what's new: {ex.Message}");
    //        }).ConfigureAwait(false);
    //    }
    //}

    /// <summary>
    /// Helper to run an action on the UI thread and await completion.
    /// The WinUI DispatcherQueue exposes TryEnqueue (bool) but not TryEnqueueAsync,
    /// so we wrap TryEnqueue with a TaskCompletionSource.
    /// </summary>
    //private Task RunOnUiThreadAsync(Action action)
    //{
    //    var tcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);

    //    // DispatcherQueue is available on ContentDialog as a property
    //    DispatcherQueue.TryEnqueue(() =>
    //    {
    //        try
    //        {
    //            action();
    //            tcs.SetResult(null);
    //        }
    //        catch (Exception ex)
    //        {
    //            tcs.SetException(ex);
    //        }
    //    });

    //    return tcs.Task;
    //}

    //private static string? TryDecodeUtf8(byte[] data)
    //{
    //    try
    //    {
    //        var s = Encoding.UTF8.GetString(data);
    //        // Heuristic: string roundtrip should produce identical bytes when re-encoded as UTF8
    //        var reencoded = Encoding.UTF8.GetBytes(s);
    //        if (reencoded.Length >= data.Length - 3) // allow some tolerance
    //            return s;
    //    }
    //    catch { }
    //    return null;
    //}
}