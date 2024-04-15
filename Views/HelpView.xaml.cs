using CryptoPortfolioTracker.Enums;
using CryptoPortfolioTracker.Models;
using CryptoPortfolioTracker.ViewModels;
using LanguageExt;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using System;
using Windows.Storage;
using Windows.Foundation.Metadata;
using Windows.Foundation;
using Microsoft.UI.Xaml.Documents;
using System.IO;
using System.Windows;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Windows.Storage.Pickers;
using WinRT;
using System.Diagnostics;

namespace CryptoPortfolioTracker.Views
{
    public partial class HelpView : Page
    {

        [ComImport, System.Runtime.InteropServices.Guid("3E68D4BD-7135-4D10-8018-9FB6D9F33FA1"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IInitializeWithWindow
        {
            void Initialize([In] IntPtr hwnd);
        }

        [DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Auto, PreserveSig = true, SetLastError = false)]
        public static extern IntPtr GetActiveWindow();


        public readonly HelpViewModel _viewModel;
        public static HelpView Current;
        
        public HelpView(HelpViewModel viewModel)
        {
            Current = this;
            _viewModel = viewModel;
            this.InitializeComponent();
            DataContext = _viewModel;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            var helpFilePath = App.appPath + "\\HelpFile.rtf"; 
            try
            {
                if (App.userPreferences.CultureLanguage == "nl") { helpFilePath = App.appPath + "\\HelpFile_NL.rtf"; }

                editor.IsReadOnly = false;
                StorageFile helpFile = await StorageFile.GetFileFromPathAsync(helpFilePath);
                Windows.Storage.Streams.IRandomAccessStream randAccStream = await helpFile.OpenAsync(Windows.Storage.FileAccessMode.Read);

                // Load the file into the Document property of the RichEditBox.
                editor.Document.LoadFromStream(Microsoft.UI.Text.TextSetOptions.FormatRtf, randAccStream);
                editor.IsReadOnly = true;
            }
            catch (Exception)
            {

            }

        }
        private async void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FileOpenPicker();
            var initializeWithWindowWrapper = picker.As<IInitializeWithWindow>();
            initializeWithWindowWrapper.Initialize(GetActiveWindow());
            picker.FileTypeFilter.Add(".rtf");
            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                try
                {
                    editor.IsReadOnly = false;
                    Windows.Storage.Streams.IRandomAccessStream randAccStream =  await file.OpenAsync(Windows.Storage.FileAccessMode.Read);

                    // Load the file into the Document property of the RichEditBox.
                    editor.Document.LoadFromStream(Microsoft.UI.Text.TextSetOptions.FormatRtf, randAccStream);
                }
                catch (Exception)
                {
                    ContentDialog errorDialog = new ContentDialog()
                    {
                        Title = "File open error",
                        XamlRoot = this.XamlRoot,
                        Content = "Sorry, I couldn't open the file.",
                        PrimaryButtonText = "Ok"
                    };

                    await errorDialog.ShowAsync();
                }
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var savePicker = new FileSavePicker();
            var initializeWithWindowWrapper = savePicker.As<IInitializeWithWindow>();
            initializeWithWindowWrapper.Initialize(GetActiveWindow());

            savePicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;

            // Dropdown of file types the user can save the file as
            savePicker.FileTypeChoices.Add("Rich Text", new List<string>() { ".rtf" });

            // Default file name if the user does not type one in or select a file to replace
            savePicker.SuggestedFileName = "New Document";

            Windows.Storage.StorageFile file = await savePicker.PickSaveFileAsync();
            
            if (file != null)
            {
                // Prevent updates to the remote version of the file until we 
                // finish making changes and call CompleteUpdatesAsync.
                Windows.Storage.CachedFileManager.DeferUpdates(file);
                // write to file
                Windows.Storage.Streams.IRandomAccessStream randAccStream =
                    await file.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite);

                editor.Document.SaveToStream(Microsoft.UI.Text.TextGetOptions.FormatRtf, randAccStream);

                // Let Windows know that we're finished changing the file so the 
                // other app can update the remote version of the file.
                Windows.Storage.Provider.FileUpdateStatus status = await Windows.Storage.CachedFileManager.CompleteUpdatesAsync(file);
                if (status != Windows.Storage.Provider.FileUpdateStatus.Complete)
                {
                    Windows.UI.Popups.MessageDialog errorBox =
                        new Windows.UI.Popups.MessageDialog("File " + file.Name + " couldn't be saved.");
                    await errorBox.ShowAsync();
                }
            }
        }
        /// <summary>
        /// hidden action to enable the editor function of the RichEditBox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RTB_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if ((sender as RichTextBlock).SelectedText == "OSFT")
            {
                editor.IsReadOnly = false;
                EditorButtons.Visibility = Visibility.Visible;
            };
        }
    }



}
