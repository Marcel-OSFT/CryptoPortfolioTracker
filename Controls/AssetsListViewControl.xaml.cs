using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CryptoPortfolioTracker.Models;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;


// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CryptoPortfolioTracker.Controls;

public partial class AssetsListViewControl : UserControl, INotifyPropertyChanged
{
    // public readonly AssetsViewModel _viewModel;

    //***********************************************//
    //** All databound fields are in the viewModel**//
    //*********************************************//
    public AssetsListViewControl()
    {
        InitializeComponent();
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

    private Brush originalBackground;

    private void SortOnNameBtn_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (sender is Button btn)
        {
            originalBackground = btn.Background;
            btn.Background = new SolidColorBrush(Colors.Orange);
            Debug.WriteLine("changed");
        }
    }

    private void SortOnNameBtn_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (sender is Button btn && originalBackground is not null)
        {
            btn.Background = originalBackground;
            Debug.WriteLine("and back");

        }
    }

    private void Control_Unload(object sender, RoutedEventArgs e)
    {
        DataContext = null;
        AssetsListView = null;
        ColumnHeadersAndListView = null;
        Root = null;
    }

}