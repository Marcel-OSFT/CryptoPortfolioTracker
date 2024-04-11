using CommunityToolkit.Mvvm.ComponentModel;
using CryptoPortfolioTracker.Models;
using CryptoPortfolioTracker.ViewModels;
using CryptoPortfolioTracker.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;


// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CryptoPortfolioTracker.Controls
{
    public partial class AssetsListViewControl : UserControl, INotifyPropertyChanged
    {
       // public readonly AssetsViewModel _viewModel;
        public static AssetsListViewControl Current;

        //***********************************************//
        //** All databound fields are in the viewModel**//
        //*********************************************//


        public AssetsListViewControl()
        {
            this.InitializeComponent();
            Current = this;
           
        }

        private void AssetsListView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (sender == null) return;
            (sender as ListView).ScrollIntoView((sender as ListView).SelectedItem);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}