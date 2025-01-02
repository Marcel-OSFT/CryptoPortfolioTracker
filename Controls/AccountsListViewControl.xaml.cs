using CryptoPortfolioTracker.ViewModels;
using CryptoPortfolioTracker.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;

namespace CryptoPortfolioTracker.Controls;

public partial class AccountsListViewControl : UserControl, INotifyPropertyChanged
{
    public readonly AccountsViewModel _viewModel;
    //private ScrollViewer gridViewScrollViewer;
    private List<ScrollViewer> gridViewScrollViewers = new List<ScrollViewer>();
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

    private void IconGridView_Loaded(object sender, RoutedEventArgs e)
    {
        var gridView = sender as GridView;
        if (gridView != null)
        {
            var scrollViewer = GetScrollViewerFromGridView(gridView);
            if (scrollViewer != null)
            {
                gridViewScrollViewers.Add(scrollViewer);
            }
        }
    }
    
    private void IconGrid_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        //// use the chached ScrollViewer to set the HorizontalScrollBarVisibility
        foreach (var scrollViewer in gridViewScrollViewers)
        {
            var pointerPosition = e.GetCurrentPoint(scrollViewer).Position;

            // Define a threshold for the sensitive area
            double threshold = 50;

            // Check if the pointer is within the threshold distance to the right edge (where the scrollbar is)
            if (pointerPosition.Y >= 0 && pointerPosition.Y < threshold)
            {
                scrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Visible;
            }
            else
            {
                scrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;
            }
        }
    }

    private void IconGrid_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        // use the chached ScrollViewer to set the HorizontalScrollBarVisibility
        foreach (var scrollViewer in gridViewScrollViewers)
        {
             scrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;
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

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

}
