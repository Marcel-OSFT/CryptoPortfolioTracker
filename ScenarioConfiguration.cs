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

using CryptoPortfolioTracker.Views;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;

namespace CryptoPortfolioTracker
{
    public partial class MainPage : Page
    {
        public const string FEATURE_NAME = "CryptoPortfolioTracker";

        List<Scenario> scenarios = new List<Scenario>
        {
            new Scenario() { Title="Assets", ClassType=typeof(AssetsView)},
            new Scenario() { Title="Accounts", ClassType=typeof(AccountsView)},
            new Scenario() { Title="Coin Library", ClassType=typeof(CoinLibraryView)},
            //new Scenario() { Title="Connect", ClassType=typeof(Scenario4_Connect)},
            //new Scenario() { Title="Manual Override", ClassType=typeof(Scenario2_ManualOverride)},
            //new Scenario() { Title="Settings", ClassType=typeof(Scenario6_Settings)}

        };
        
    }

    public class Scenario
    {
        public string Title { get; set; }
        public Type ClassType { get; set; }
    }
}
