//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// This code is licensed under the MIT License (MIT).
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************

using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using Windows.Devices.WiFi;


namespace CryptoPortfolioTracker
{
    public sealed partial class Scenario1_MyCoinLibrary : Page
    {
        
        public static Scenario1_MyCoinLibrary Current;
      

        public Scenario1_MyCoinLibrary()
        {
            this.InitializeComponent();
            Current = this;
            
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {

            DataContext = this;
            progressIndicator.IsActive = true;
              progressIndicator.IsActive = false;
            
        }
        private void Button_Click_AddCoin(object sender, RoutedEventArgs e)
        {
            _ = AddCoinByDialog();
        }
        private async Task AddCoinByDialog()
        {
            ContentDialog dialog = new ContentDialog();

            // XamlRoot must be set in the case of a ContentDialog running in a Desktop app
            dialog.XamlRoot = this.XamlRoot;
            dialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
            dialog.Title = "Add a coin to your Library";
            dialog.PrimaryButtonText = "Accept";
            //dialog.SecondaryButtonText = "Don't Save";
            dialog.CloseButtonText = "Cancel";
            dialog.DefaultButton = ContentDialogButton.Primary;
            TextBox txtBox = new TextBox();
            dialog.Content = txtBox;

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
               
            }
            else
            {
               
            }
        }
    }

}
