using CryptoPortfolioTracker.Models;
using CryptoPortfolioTracker.ViewModels;
using LanguageExt;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;

namespace CryptoPortfolioTracker.Controls;

public partial class NarrativesListViewControl : UserControl, INotifyPropertyChanged
{
    public readonly NarrativesViewModel _viewModel;

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
            listView.ScrollIntoView(listView.SelectedItem);
        }
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

   
   
}
