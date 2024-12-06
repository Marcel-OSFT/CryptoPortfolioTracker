using CommunityToolkit.Mvvm.DependencyInjection;
using CryptoPortfolioTracker.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace CryptoPortfolioTracker.Views
{
    public sealed partial class PriceLevelsView : Page
    {
        public PriceLevelsViewModel ViewModel { get; }

        public PriceLevelsView()
        {
            this.InitializeComponent();
            this.ViewModel = Ioc.Default.GetRequiredService<PriceLevelsViewModel>();
        }


    }
}
