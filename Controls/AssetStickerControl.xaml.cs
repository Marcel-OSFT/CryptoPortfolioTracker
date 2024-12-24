using CryptoPortfolioTracker.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.ComponentModel;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;

namespace CryptoPortfolioTracker.Controls;

public partial class AssetStickerControl : UserControl, INotifyPropertyChanged
{

    //***********************************************//
    //** All databound fields are in the viewModel**//
    //*********************************************//

    public AssetStickerControl()
    {
        InitializeComponent();
       // _viewModel = HeatMapControlViewModel.Current;
       // DataContext = _viewModel;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

}
