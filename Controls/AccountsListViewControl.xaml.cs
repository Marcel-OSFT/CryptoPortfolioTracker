using CryptoPortfolioTracker.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.ComponentModel;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;

namespace CryptoPortfolioTracker.Controls;

public partial class AccountsListViewControl : UserControl, INotifyPropertyChanged
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


    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void IconGrid_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        GridView gridView = sender as GridView;
        if (gridView != null)
        {
            ScrollViewer scrollViewer = GetScrollViewerFromGridView(gridView);
            if (scrollViewer != null)
            {
                scrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Visible;
            }
        }
    }

    private void IconGrid_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        GridView gridView = sender as GridView;
        if (gridView != null)
        {
            ScrollViewer scrollViewer = GetScrollViewerFromGridView(gridView);
            if (scrollViewer != null)
            {
                scrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;
            }
        }
    }

    private ScrollViewer GetScrollViewerFromGridView(GridView gridView)
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(gridView); i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(gridView, i);
            if (child is ScrollViewer scrollViewer)
            {
                return scrollViewer;
            }
            else
            {
                ScrollViewer descendant = GetScrollViewerFromGridView(child);
                if (descendant != null)
                {
                    return descendant;
                }
            }
        }
        return null;
    }

    private ScrollViewer GetScrollViewerFromGridView(DependencyObject element)
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(element, i);
            if (child is ScrollViewer scrollViewer)
            {
                return scrollViewer;
            }
            else
            {
                ScrollViewer descendant = GetScrollViewerFromGridView(child);
                if (descendant != null)
                {
                    return descendant;
                }
            }
        }
        return null;
    }

}
