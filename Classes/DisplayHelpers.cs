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
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.ComponentModel;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using Windows.Devices.WiFi;
using Windows.Networking.Connectivity;

namespace CryptoPortfolioTracker
{
    public class WiFiNetworkDisplay : INotifyPropertyChanged
    {
        private WiFiAdapter adapter;
        ConnectionProfile connectedProfile;
        public WiFiNetworkDisplay(WiFiAvailableNetwork availableNetwork, WiFiAdapter adapter)
        {
            AvailableNetwork = availableNetwork;
            this.adapter = adapter;
            UpdateWiFiImage();

        }

        private void UpdateWiFiImage()
        {
            string imageFileNamePrefix = "secure";
            if (AvailableNetwork.SecuritySettings.NetworkAuthenticationType == NetworkAuthenticationType.Open80211)
            {
                imageFileNamePrefix = "open";
            }

            string imageFileName = string.Format("ms-appx:/Assets/{0}_{1}bar.png", imageFileNamePrefix, AvailableNetwork.SignalBars);

            WiFiImage = new BitmapImage(new Uri(imageFileName));

            OnPropertyChanged("WiFiImage");


        }

        //******** public async Task UpdateConnectivityLevel()
        public void UpdateConnectivityLevel()
        {
            string connectivityLevel = NetworkConnectivityLevel.None.ToString();
            string connectedSsid = null;

            //****** var connectedProfile = await adapter.NetworkAdapter.GetConnectedProfileAsync();
            connectedProfile = NetworkInformation.GetInternetConnectionProfile();
            if (connectedProfile != null &&
                connectedProfile.IsWlanConnectionProfile &&
                connectedProfile.WlanConnectionProfileDetails != null)
            {
                connectedSsid = connectedProfile.WlanConnectionProfileDetails.GetConnectedSsid();

            }

            if (!string.IsNullOrEmpty(connectedSsid))
            {
                if (connectedSsid.Equals(AvailableNetwork.Ssid))
                {
                    try
                    {
                        connectivityLevel = connectedProfile.GetNetworkConnectivityLevel().ToString();
                        //connectivityLevel = GetWifiConnectivityLevel().ToString();
                        UpdateUsageData();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
            }

            ConnectivityLevel = connectivityLevel;

            OnPropertyChanged("ConnectivityLevel");
        }
        public NetworkConnectivityLevel GetWifiConnectivityLevel()
        {
            string host = "www.google.com";
            //if this function is called it means that there is a Wifi connection UP both could be only local acces.
            NetworkConnectivityLevel connectivityLevel = NetworkConnectivityLevel.LocalAccess;

            Ping p = new Ping();
            try
            {
                PingReply reply = p.Send(host, 3000);
                if (reply.Status == IPStatus.Success)
                {
                    connectivityLevel = NetworkConnectivityLevel.InternetAccess;
                }
            }
            catch { }
            return connectivityLevel;
        }

        public String Ssid
        {
            get
            {
                string ssid = availableNetwork.Ssid;
                if (ssid == "")
                {
                    ssid = "Unknown";
                }
                return ssid;
            }
        }

        public String Bssid
        {
            get
            {
                return availableNetwork.Bssid;

            }
        }

        public String ChannelCenterFrequency
        {
            get
            {
                return string.Format("{0}kHz", availableNetwork.ChannelCenterFrequencyInKilohertz);
            }
        }

        public String Rssi
        {
            get
            {
                return string.Format("{0}dBm", availableNetwork.NetworkRssiInDecibelMilliwatts);
            }
        }

        public String SecuritySettings
        {
            get
            {
                return string.Format("Authentication: {0}; Encryption: {1}", availableNetwork.SecuritySettings.NetworkAuthenticationType, availableNetwork.SecuritySettings.NetworkEncryptionType);
            }
        }

        private string bytesReceived = "no data";
        private string bytesSent = "no data";
        private string connectionDuration = "no data";
        private string inboundBitsPerSecond = "no data";
        private void UpdateUsageData()
        {

            Task taskA = Task.Run(async () =>
            {
                var now = DateTime.Now;
                var states = new NetworkUsageStates { Roaming = TriStates.DoNotCare, Shared = TriStates.DoNotCare };

                var usages = await connectedProfile.GetNetworkUsageAsync(now.AddDays(-2), now, DataUsageGranularity.PerDay, states);
                bytesReceived = $"BytesReceived:  {usages[0].BytesReceived.ToString()}";
                bytesSent = $"BytesSend:  {usages[0].BytesSent.ToString()}";
                connectionDuration = $"Connection duration:  {usages[0].ConnectionDuration.ToString()}";

            });
            taskA.Wait();

            OnPropertyChanged("BytesReceived");
            OnPropertyChanged("BytesSent");
            OnPropertyChanged("ConnectionDuration");
            OnPropertyChanged("InboundBitsPerSecond");
        }

        public String BytesReceived
        {
            get
            {
                return bytesReceived;
            }
        }
        public String BytesSent
        {
            get
            {
                return bytesSent;
            }
        }
        public String ConnectionDuration
        {
            get
            {
                return connectionDuration;
            }
        }
        public String InboundBitsPerSecond
        {
            get
            {
                return inboundBitsPerSecond;
            }
        }

        public String ConnectivityLevel
        {
            get;
            private set;
        }

        public BitmapImage WiFiImage
        {
            get;
            private set;
        }


        private WiFiAvailableNetwork availableNetwork;
        public WiFiAvailableNetwork AvailableNetwork
        {
            get
            {
                return availableNetwork;
            }

            private set
            {
                availableNetwork = value;
            }
        }




        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            MainPage.Current.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
            {

                PropertyChangedEventHandler handler = PropertyChanged;
                if (handler != null)
                {
                    handler(this, new PropertyChangedEventArgs(name));
                }
                // new PropertyChangedEventArgs(name);


            });
        }
    }
}
