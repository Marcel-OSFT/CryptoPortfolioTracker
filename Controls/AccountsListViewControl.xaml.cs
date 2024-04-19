using CryptoPortfolioTracker.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;



// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CryptoPortfolioTracker.Controls
{
    public partial class AccountsListViewControl : UserControl
    {
        public readonly AccountsViewModel _viewModel;
        public static AccountsListViewControl Current;

        private bool isAssetListShown;

        //***********************************************//
        //** All databound fields are in the viewModel**//
        //*********************************************//

        public AccountsListViewControl()
        {
            this.InitializeComponent();
            Current = this;
            _viewModel = AccountsViewModel.Current;
            DataContext = _viewModel;
        }

        private void ListView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (sender == null) return;
            (sender as ListView).ScrollIntoView((sender as ListView).SelectedItem);
        }
    }
}
