using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

using Newtonsoft.Json;
using CoinGecko.Clients;
using System.Net.Http;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CryptoPortfolioTracker
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {

        string strTest;

        public MainWindow()
        {
            this.InitializeComponent();
            //myButton.Content = "Click me";

        }

        public async void Test()
        {
            //myTextBox.Text = "Requesting...";
            HttpClient httpClient = new HttpClient();
            JsonSerializerSettings serializerSettings = new JsonSerializerSettings();

            PingClient pingClient = new PingClient(httpClient, serializerSettings);
            SimpleClient simpleClient = new SimpleClient(httpClient, serializerSettings);

            // Check CoinGecko API status
            if ((await pingClient.GetPingAsync()).GeckoSays != string.Empty)
            {
                // Getting current price of tether in usd
                string ids = "bitcoin";
                string vsCurrencies = "usd";
                strTest = (await simpleClient.GetSimplePrice(new[] { ids }, new[] { vsCurrencies }))[ids]["usd"].ToString();
                //Console.WriteLine((await simpleClient.GetSimplePrice(new[] { ids }, new[] { vsCurrencies }))["tether"]["usd"]);
                //myTextBox.Text = strTest;
            }
        }



        //private async void mybutton_click(object sender, routedeventargs e)
        //{


        //    test();

        //}
    }
}
