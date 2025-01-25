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
        public bool IsInitialPortfolioLoaded { get; }
        [ObservableProperty] private Portfolio selectedPortfolio;

        public SwitchPortfolioViewModel(PortfolioService portfolioService, IPreferencesService preferencesService) : base(preferencesService)
        {
            Logger = Log.Logger.ForContext(Constants.SourceContextPropertyName, typeof(AccountsViewModel).Name.PadRight(22));
            Current = this;
            _preferencesService = preferencesService;
            _portfolioService = portfolioService;
            SelectedPortfolio = _portfolioService.CurrentPortfolio;
            IsInitialPortfolioLoaded = _portfolioService.IsInitialPortfolioLoaded;
        }

        public async Task<Result<bool>> SwitchPortfolioAsync(Portfolio newValue)
        {
            //*** Pause update services and wait till they finsihed their update cycle
            //*** before switching the context


            await _portfolioService.PauseUpdateServices();

            //*** Update Services are paused and not updating
            //*** Switch the database context

            var switchResult = await _portfolioService.SwitchPortfolio(newValue);
            return await switchResult.Match(
                Succ: async succ =>
                {
                    //*** Resume update services
                    SelectedPortfolio = newValue;
                    _portfolioService.ResumeUpdateServices();
                    return new Result<bool>(true);
                },
                Fail: async err =>
                {
                    //*** Resume update services
                    //selectedPortfolio remains the same
                    _portfolioService.ResumeUpdateServices();
                    return new Result<bool>(err);
                });
           
        }

    }
}
