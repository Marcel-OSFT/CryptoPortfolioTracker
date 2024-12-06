using CryptoPortfolioTracker.ViewModels;
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

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CryptoPortfolioTracker.Controls
{
    public sealed partial class PriceLevelsListViewControl : UserControl
    {
        public readonly PriceLevelsViewModel _viewModel;

        //***********************************************//
        //** All databound fields are in the viewModel**//
        //*********************************************//
        public PriceLevelsListViewControl()
        {
            InitializeComponent();
            _viewModel = PriceLevelsViewModel.Current;
            DataContext = _viewModel;
        }
    }
}
