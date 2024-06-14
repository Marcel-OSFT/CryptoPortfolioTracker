using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CommunityToolkit.WinUI;
using CryptoPortfolioTracker.Models;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinUI3Localizer;

namespace CryptoPortfolioTracker.Controls;

public partial class AssetsListViewControl : UserControl, INotifyPropertyChanged
{
    private int selectedItemIndex;
    private bool isSettingIndex;

    //***********************************************//
    //** All databound fields are in the viewModel**//
    //*********************************************//
    public AssetsListViewControl()
    {
        InitializeComponent();
    }

    private void AssetsListView_Loading(FrameworkElement sender, object args)
    {
        selectedItemIndex = 0;
    }
    
    private void AssetsListView_SizeChanged(object sender, SizeChangedEventArgs e)
    {

        if (sender is ListView listView)
        {
            listView.ScrollIntoView(listView.SelectedItem);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private async void View_ItemClick(object sender, ItemClickEventArgs e)
    {
       //dummy
    }

    /// <summary>
    /// Changing selection will be monitored. After sorting a list, the SelectedItem is set to NULL
    /// After an price update thelist is automatically sorted again. An eventual slected Item will be 
    /// 'lost' and can't be kept visible in the list. 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void AssetsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ListView listView)
        {
            if (listView.SelectedItem is AssetTotals asset)
            {
                selectedItemIndex = listView.SelectedIndex;

                if (isSettingIndex)
                {
                    listView.ScrollIntoView(listView.SelectedItem, ScrollIntoViewAlignment.Leading);
                    isSettingIndex = false;
                }
            }
            else
            {
                isSettingIndex = true;
                listView.SelectedIndex = selectedItemIndex;
            }
        }
    }

    
}