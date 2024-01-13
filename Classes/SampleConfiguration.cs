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
            new Scenario() { Title="My Coin Library", ClassType=typeof(Scenario1_MyCoinLibrary)},
            //new Scenario() { Title="Scan", ClassType=typeof(Scenario2_Scan)},
            //new Scenario() { Title="Register for Scan Updates", ClassType=typeof(Scenario3_RegisterForUpdates)},
            //new Scenario() { Title="Connect", ClassType=typeof(Scenario4_Connect)},
            //new Scenario() { Title="Manual Override", ClassType=typeof(Scenario2_ManualOverride)},
            //new Scenario() { Title="Settings", ClassType=typeof(Scenario6_Settings)}

        };
        //List<Scenario> SettingsScenarios = new List<Scenario>
        //{
        //    new Scenario() { Title="Settings1_Hotspots", ClassType=typeof(Settings1_Hotspots)},
        //    new Scenario() { Title="Settings2_Priorities", ClassType=typeof(Settings2_Priorities)},
        //    new Scenario() { Title="Settings3_Rules", ClassType=typeof(Settings3_Rules)}
        //};



    }

    public class Scenario
    {
        public string Title { get; set; }
        public Type ClassType { get; set; }
    }
}
