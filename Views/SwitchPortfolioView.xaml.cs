using CommunityToolkit.Mvvm.DependencyInjection;
using CryptoPortfolioTracker.Helpers;
using CryptoPortfolioTracker.Models;
using CryptoPortfolioTracker.Services;
using CryptoPortfolioTracker.ViewModels;
using LanguageExt.Common;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI;
using static WinUI3Localizer.LanguageDictionary;

namespace CryptoPortfolioTracker.Views
{
    public partial class SwitchPortfolioView : Page
    {
        public SwitchPortfolioViewModel _viewModel { get; }
        public static SwitchPortfolioView Current;
        private Portfolio? selectedPortfolio;
        private Portfolio? deselectedPortfolio;
        private bool animationBusy = false;

        public SwitchPortfolioView(SwitchPortfolioViewModel viewModel)
        {
            Current = this;
            _viewModel = viewModel;
            InitializeComponent();
            DataContext = _viewModel;
        }

        private async void PortfoliosListView_DoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
        {
            if (sender is not ListView listView) return;

            var tappedPortfolio = listView.SelectedItem as Portfolio;
            if (tappedPortfolio == selectedPortfolio) return;

            deselectedPortfolio = selectedPortfolio;
            selectedPortfolio = tappedPortfolio;

            await AnimatePortfolio(Colors.Transparent, listView, deselectedPortfolio);

            var switchResult = await _viewModel.SwitchPortfolioAsync(selectedPortfolio);

            await switchResult.Match(
                Succ: async _ =>
                {
                    await AnimatePortfolio(Colors.Green, listView, selectedPortfolio, true);
                },
                Fail: async _ =>
                {
                    await AnimatePortfolio(Colors.Red, listView, selectedPortfolio);

                    // Revert back to the previous selected portfolio
                    selectedPortfolio = deselectedPortfolio;
                    deselectedPortfolio = tappedPortfolio;
                    await AnimatePortfolio(Colors.Green, listView, selectedPortfolio, true);
                });
        }

        private async Task AnimatePortfolio(Color color, ListView listView, Portfolio? portfolio, bool animateTitle = false)
        {
            var container = (listView.ContainerFromItem(portfolio)) as ListViewItem;

            if (container != null)
            {
                var border = MkOsft.GetElementFromUiElement<Border>(container, "ItemBorder");

                // Set the alpha to 0.2
                if (color != Colors.Transparent) color.A = (byte)(0.2 * 255);

                var storyboard = new Storyboard()
                {
                    //Duration = new Duration(TimeSpan.FromSeconds(1)),
                    Children =
                        {
                            new ColorAnimation()
                            {
                                To = color,
                                Duration = new Duration(TimeSpan.FromSeconds(0.5)),
                                EnableDependentAnimation = true,
                                EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseInOut }
                            },
                        }
                };
                Storyboard.SetTarget(storyboard.Children[0], border);
                Storyboard.SetTargetProperty(storyboard.Children[0], "(Border.Background).(SolidColorBrush.Color)");

                if (animateTitle)
                {
                    var fontSizeAnimation = new DoubleAnimation()
                    {
                        From = 16,
                        To = 50,
                        Duration = new Duration(TimeSpan.FromSeconds(1)),
                        AutoReverse = true,
                        EnableDependentAnimation = true,
                        EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseInOut }
                    };
                    var title = MkOsft.GetElementFromUiElement<TextBlock>(this, "txtPortfolio");

                    Storyboard.SetTarget(fontSizeAnimation, title);
                    Storyboard.SetTargetProperty(fontSizeAnimation, "(TextBlock.FontSize)");
                    storyboard.Children.Add(fontSizeAnimation);

                    // Create the TranslateTransform animations
                    DoubleAnimation moveXAnimation = new DoubleAnimation
                    {
                        To = (HeaderGrid.ActualWidth / 2) - (title.ActualWidth / 2),
                        Duration = new Duration(TimeSpan.FromSeconds(1)),
                        AutoReverse = true
                    };
                    Storyboard.SetTarget(moveXAnimation, title.RenderTransform);
                    Storyboard.SetTargetProperty(moveXAnimation, "X");
                    storyboard.Children.Add(moveXAnimation);

                    DoubleAnimation moveYAnimation = new DoubleAnimation
                    {
                        To = (HeaderGrid.ActualHeight / 4) - (title.ActualHeight / 2),
                        Duration = new Duration(TimeSpan.FromSeconds(1)),
                        AutoReverse = true
                    };
                    Storyboard.SetTarget(moveYAnimation, title.RenderTransform);
                    Storyboard.SetTargetProperty(moveYAnimation, "Y");
                    storyboard.Children.Add(moveYAnimation);

                }
                
                storyboard.Completed += Storyboard_Completed;

                animationBusy = true;
                storyboard?.Begin();

                while (animationBusy)
                {
                    await Task.Delay(100);
                }
            }
        }

        private void Storyboard_Completed(object? sender, object e)
        {
            animationBusy = false;
        }

        private async void ListView_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not ListView listView) return;

            listView.ItemsSource = _viewModel._portfolioService.Portfolios;

            var selectedIndex = listView.Items
                .Cast<Portfolio>()
                .ToList()
                .FindIndex(p => p.Name == _viewModel.SelectedPortfolio.Name);

            if (selectedIndex == -1) return;

            listView.SelectedIndex = selectedIndex;
            selectedPortfolio = listView.SelectedItem as Portfolio;

            var color = _viewModel.IsInitialPortfolioLoaded ? Colors.Green : Colors.Red;
            await AnimatePortfolio(color, listView, selectedPortfolio);
        }
    }
}
