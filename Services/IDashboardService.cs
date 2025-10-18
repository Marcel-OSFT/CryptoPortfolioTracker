using CryptoPortfolioTracker.Controls;
using CryptoPortfolioTracker.Enums;
using CryptoPortfolioTracker.Infrastructure;
using CryptoPortfolioTracker.Models;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView.WinUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryptoPortfolioTracker.Services
{
    public interface IDashboardService
    {
        public Coin GetPriceLevelsFromContext(Coin coin);
        public Task<ObservableCollection<PiePoint>> GetPiePoints(string pieChartName);
        Task<List<Coin>> GetTopWinners();
        Task<List<Coin>> GetTopLosers();
        double GetPortfolioValue();
        double GetCostBase();
        Task<List<CapitalFlowPoint>> GetYearlyMutationsByTransactionKind(TransactionKind transactionKind);
        PortfolioContext GetContext();
        Portfolio GetPortfolio();
        Task CalculateIndicatorsAllCoins();
        Task EvaluatePriceLevels();
        Task CalculateRsiAllCoins();
        Task CalculateMaAllCoins();
    }
}
