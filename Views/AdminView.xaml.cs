using CommunityToolkit.Mvvm.Messaging;
using CryptoPortfolioTracker.Helpers;
using CryptoPortfolioTracker.Models;
using CryptoPortfolioTracker.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI;

namespace CryptoPortfolioTracker.Views
{
    public class ShowAnimationMessage
    {
        public Color color;
        public bool onlyPrimary;
        public Portfolio portfolioToAnimate;
        public ShowAnimationMessage(Color _color, bool _onlyPrimary = false, Portfolio _portfolioToAnimate = null)
        {
            color = _color;
            onlyPrimary = _onlyPrimary;
            portfolioToAnimate = _portfolioToAnimate;
        }
        
    }

    public partial class AdminView : Page
    {
        public AdminViewModel _viewModel { get; }
        
        public static AdminView Current;

        public AdminView(AdminViewModel adminViewModel, IMessenger messenger)
        {
            Current = this;
            InitializeComponent();
            _viewModel = adminViewModel;
            DataContext = _viewModel;
            messenger.Register<ShowAnimationMessage>(this, (r, m) =>
            {
                if (m.onlyPrimary)
                {
                    PortfolioListView.SelectedItem = PortfolioListView.Items
                        .OfType<Portfolio>()
                        .FirstOrDefault(portfolio => portfolio.Name == m.portfolioToAnimate.Name);
                    AnimateSelection(m.color, PortfolioListView);
                }
                else
                {
                    AnimateSelection(m.color, PortfolioListView, BackupListView);
                }
            });
        }

        private void Page_Loading(FrameworkElement sender, object args)
        {
            _viewModel.AdminViewLoading();
        }

        private void AnimateSelection(Color color, ListView primaryListView, ListView? secondaryListView = null)
        {
            var primaryItem = (ListViewItem)primaryListView.ContainerFromItem(primaryListView.SelectedItem);
            var secondaryItem = (ListViewItem)secondaryListView?.ContainerFromItem(secondaryListView.SelectedItem);

            if (primaryItem != null)
            {
                var primaryBorder = MkOsft.GetElementFromUiElement<Border>(primaryItem, "PortfolioItem");
                var primaryStoryboard = CreateColorAnimation(Colors.Transparent, color, 2, primaryBorder);
                primaryStoryboard.Completed += (s, e) =>
                {
                    primaryBorder.Background = new SolidColorBrush(Colors.Transparent);
                };

                if (secondaryItem != null)
                {
                    var secondaryBorder = MkOsft.GetElementFromUiElement<Border>(secondaryItem, "BackupItem");
                    var secondaryStoryboard = CreateColorAnimation(color, Colors.Transparent, 2, secondaryBorder);
                    
                    secondaryStoryboard.Completed += (s, e) =>
                    {
                        secondaryBorder.Background = new SolidColorBrush(Colors.Transparent);
                    };
                    secondaryStoryboard.Begin();
                }
                primaryStoryboard.Begin();
            }
        }

        private Storyboard CreateColorAnimation(Color fromColor, Color toColor, int repeatCount, Border target)
        {
            var colorAnimation = new ColorAnimation
            {
                From = fromColor,
                To = toColor,
                Duration = new Duration(TimeSpan.FromSeconds(2)),
                AutoReverse = true,
                RepeatBehavior = new RepeatBehavior(repeatCount)
            };

            var storyboard = new Storyboard();

            storyboard.Children.Add(colorAnimation);
            Storyboard.SetTarget(storyboard.Children[0], target);
            Storyboard.SetTargetProperty(storyboard.Children[0], "(Border.Background).(SolidColorBrush.Color)");

            return storyboard;
        }


        private void PortfolioListView_Loaded(object sender, RoutedEventArgs e)
        {
            _viewModel.DeletePortfolioCommand.NotifyCanExecuteChanged();
        }

        private void BackupListView_Loading(FrameworkElement sender, object args)
        {
            
        }

        private void PortfolioListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is not ListView listView) return;
            BackupListView.SelectedIndex = BackupListView.Items.Any() ? 0 : -1;
        }

        private void BackupListView_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not ListView listView) return;
            listView.SelectedIndex = listView.Items.Any() ? 0 : -1;
        }

        private void CloseFlyoutDelete(object sender, RoutedEventArgs e)
        {
            AppBarButton appBarBtn = MkOsft.GetElementFromUiElement<AppBarButton>(MainGrid, "DeleteFlyoutAbb");

            var flyout = appBarBtn.Flyout;
            flyout.Hide();
        }
        private void CloseFlyoutRestore(object sender, RoutedEventArgs e)
        {
            AppBarButton appBarBtn = MkOsft.GetElementFromUiElement<AppBarButton>(MainGrid, "RestoreFlyoutAbb");

            var flyout = appBarBtn.Flyout;
            flyout.Hide();
        }

    }
}
