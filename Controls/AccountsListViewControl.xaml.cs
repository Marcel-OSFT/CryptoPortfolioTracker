using CryptoPortfolioTracker.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

namespace CryptoPortfolioTracker.Controls;

public partial class AccountsListViewControl : UserControl
{
    public readonly AccountsViewModel _viewModel;

    //***********************************************//
    //** All databound fields are in the viewModel**//
    //*********************************************//

    public AccountsListViewControl()
    {
        InitializeComponent();
        _viewModel = AccountsViewModel.Current;
        DataContext = _viewModel;
    }

    private void ListView_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (sender is ListView listView)
        {
            listView.ScrollIntoView(listView.SelectedItem);
        }
    }

    private void Control_Unload(object sender, RoutedEventArgs e)
    {
        DataContext = null;
        AccountsListView = null;
        ColumnHeadersAndListView = null;
    }

    
}
