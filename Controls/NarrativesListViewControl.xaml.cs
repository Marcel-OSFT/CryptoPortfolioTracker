using CryptoPortfolioTracker.Models;
using CryptoPortfolioTracker.ViewModels;
using Flurl.Util;
using LanguageExt;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;

namespace CryptoPortfolioTracker.Controls;

public partial class NarrativesListViewControl : UserControl, INotifyPropertyChanged
{
    public readonly NarrativesViewModel _viewModel;
    private readonly List<ScrollViewer> gridViewScrollViewers = new();

    //***********************************************//
    //** All databound fields are in the viewModel**//
    //*********************************************//

    public NarrativesListViewControl()
    {
        InitializeComponent();
        _viewModel = NarrativesViewModel.Current;
        DataContext = _viewModel;
        SetupTeachingTips();
    }

    private void SetupTeachingTips()
    {
        var teachingTipInitial = _viewModel._preferencesService.GetTeachingTip("TeachingTipBlank");
        var teachingTipNarr = _viewModel._preferencesService.GetTeachingTip("TeachingTipNarrNarr");

        if (teachingTipInitial == null || !teachingTipInitial.IsShown)
        {
            _viewModel._preferencesService.SetTeachingTipAsShown("TeachingTipNarrNarr");
        }
        else if (teachingTipNarr != null && !teachingTipNarr.IsShown)
        {
            MyTeachingTipNarr.IsOpen = true;
        }
    }

    private void ListView_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (sender is ListView listView)
        {
            KeepIntoView(listView);
        }
    }

    private void KeepIntoView(ListView listView)
    {
        if (listView == null || DataContext is not NarrativesViewModel viewModel || !viewModel.IsExtendedView)
            return;

        if (viewModel.selectedNarrative != null && listView.SelectedItem != viewModel.selectedNarrative)
        {
            listView.SelectedItem = viewModel.selectedNarrative;
        }
        listView.ScrollIntoView(listView.SelectedItem, ScrollIntoViewAlignment.Leading);
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

    private async void OnGetItClickedNarr(object sender, RoutedEventArgs e)
    {
        // Handle the 'Get it' button click
        MyTeachingTipNarr.IsOpen = false;
        _viewModel._preferencesService.SetTeachingTipAsShown("TeachingTipNarrNarr");

        var narrative = NarrativesListView.Items
            .OfType<Narrative>()
            .FirstOrDefault(n => n.Coins != null && n.Coins.Any());

        if (narrative != null)
        {
            await _viewModel.NarrativeItemClicked(narrative);
        }

        // Navigate to the new feature or provide additional information
    }

    private void OnDismissClickedNarr(object sender, RoutedEventArgs e)
    {
        MyTeachingTipNarr.IsOpen = false;
        _viewModel._preferencesService.SetTeachingTipAsShown("TeachingTipNarrNarr");

    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private async void IconGrid_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (sender is FrameworkElement frameworkElement && frameworkElement.Parent is FrameworkElement parentElement)
        {
            var narrative = parentElement.Tag as Narrative;
            if (narrative != null)
            {
                await _viewModel.NarrativeItemClicked(narrative);
            }
        }
    }
}
