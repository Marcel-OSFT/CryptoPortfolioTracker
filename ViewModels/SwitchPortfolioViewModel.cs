using CommunityToolkit.Mvvm.ComponentModel;
using CryptoPortfolioTracker.Controls;
using CryptoPortfolioTracker.Models;
using CryptoPortfolioTracker.Services;
using Serilog;
using System.Threading.Tasks;
using System;
using Serilog.Core;
using CryptoPortfolioTracker.Views;
using System.Diagnostics;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using LanguageExt.Common;

namespace CryptoPortfolioTracker.ViewModels
{
    public partial class SwitchPortfolioViewModel : BaseViewModel
    {
        public static SwitchPortfolioViewModel Current;
        private readonly IPreferencesService _preferencesService;
        public PortfolioService _portfolioService { get; private set; }
        private readonly IPriceUpdateService _priceUpdateService;
        public bool IsInitialPortfolioLoaded { get; }
        private readonly IGraphUpdateService _graphUpdateService;
        [ObservableProperty] private Portfolio selectedPortfolio;

        public SwitchPortfolioViewModel(PortfolioService portfolioService, 
            IPriceUpdateService priceUpdateService,
            IGraphUpdateService graphUpdateService,
            IPreferencesService preferencesService) : base(preferencesService)
        {
            Logger = Log.Logger.ForContext(Constants.SourceContextPropertyName, typeof(AccountsViewModel).Name.PadRight(22));
            Current = this;
            _preferencesService = preferencesService;
            _portfolioService = portfolioService;
            SelectedPortfolio = _portfolioService.CurrentPortfolio;
            IsInitialPortfolioLoaded = _portfolioService.IsInitialPortfolioLoaded;

            _graphUpdateService = graphUpdateService;
            _priceUpdateService = priceUpdateService;
        }

        public async Task<Result<bool>> SwitchPortfolioAsync(Portfolio newValue)
        {
            //*** Pause update services and wait till they finsihed their update cycle
            //*** before switching the context
            _priceUpdateService.Pause();
            _graphUpdateService.Pause();

            while (_priceUpdateService.IsUpdating || _graphUpdateService.IsUpdating)
            {
                await Task.Delay(100);
            }

            //*** Update Services are paused and not updating
            //*** Switch the database context

            var switchResult = await _portfolioService.SwitchPortfolio(newValue);
            return await switchResult.Match(
                Succ: async succ =>
                {
                    //*** Resume update services
                    SelectedPortfolio = newValue;
                    await _priceUpdateService.Resume();
                    await _graphUpdateService.Resume();
                    return new Result<bool>(true);
                },
                Fail: async err =>
                {
                    //*** Resume update services
                    //selectedPortfolio remains the same
                    await _priceUpdateService.Resume();
                    await _graphUpdateService.Resume();
                    return new Result<bool>(err);
                });
           
        }

        private async Task ShowMessage(string message)
        {
            var dialog = new ContentDialog
            {
                Title = "Portfolio Action",
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = MainPage.Current.XamlRoot // Ensure this line is correct for your WinUI version
            };
            XamlRoot xmlroot = dialog.XamlRoot;

            await dialog.ShowAsync();
        }

    }
}
