using CryptoPortfolioTracker.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace CryptoPortfolioTracker.Controls;

public partial class AccountsListViewControl : UserControl
{
    public readonly AccountsViewModel _viewModel;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public static AccountsListViewControl Current;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    //***********************************************//
    //** All databound fields are in the viewModel**//
    //*********************************************//

    public AccountsListViewControl()
    {
        InitializeComponent();
        Current = this;
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
}
