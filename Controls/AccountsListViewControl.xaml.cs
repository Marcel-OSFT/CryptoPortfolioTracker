using CommunityToolkit.Mvvm.ComponentModel;
using CryptoPortfolioTracker.Models;
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

[ObservableObject]
public partial class AccountsListViewControl : UserControl
{
    public readonly AccountsViewModel _viewModel;
    //private ScrollViewer gridViewScrollViewer;
    private readonly List<ScrollViewer> gridViewScrollViewers = new();
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
        var listView = sender as ListView;
        if (listView != null)
        {
            KeepIntoView(listView);
        }
    }

    private void KeepIntoView(ListView listView)
    {
        if (listView?.DataContext is AccountsViewModel viewModel && viewModel.IsExtendedView)
        {
            if (viewModel.selectedAccount != null && listView.SelectedItem != viewModel.selectedAccount)
            {
                listView.SelectedItem = viewModel.selectedAccount;
            }
            listView.ScrollIntoView(listView.SelectedItem, ScrollIntoViewAlignment.Leading);
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
        if (sender is GridView gridView && gridView.Items.Count == 0) return;

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
        if (sender is GridView gridView && gridView.Items.Count == 0) return;
        // use the chached ScrollViewer to set the HorizontalScrollBarVisibility
        foreach (var scrollViewer in gridViewScrollViewers)
        {
             scrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;
        }
    }


    private ScrollViewer? GetScrollViewerFromGridView(GridView gridView)
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
                ScrollViewer? descendant = GetScrollViewerFromGridView(child);
                if (descendant != null)
                {
                    return descendant;
                }
            }
        }
        return null;
    }

    private ScrollViewer? GetScrollViewerFromGridView(DependencyObject element)
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
                ScrollViewer? descendant = GetScrollViewerFromGridView(child);
                if (descendant != null)
                {
                    return descendant;
                }
            }
        }
        return null;
    }

    private async void IconGrid_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (sender is FrameworkElement frameworkElement && frameworkElement.Parent is FrameworkElement parentElement)
        {
            var account = parentElement.Tag as Account;
            if (account != null)
            {
                await _viewModel.AccountItemClicked(account);
            }
        }
    }

    //public event PropertyChangedEventHandler? PropertyChanged;

    //protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    //{
    //    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    //}

}
