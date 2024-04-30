using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;


// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CryptoPortfolioTracker.Controls;

public partial class AssetsListViewControl : UserControl, INotifyPropertyChanged
{
    // public readonly AssetsViewModel _viewModel;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public static AssetsListViewControl Current;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    //***********************************************//
    //** All databound fields are in the viewModel**//
    //*********************************************//
    public AssetsListViewControl()
    {
        InitializeComponent();
        Current = this;
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
}