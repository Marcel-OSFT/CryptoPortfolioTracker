using CryptoPortfolioTracker.Controls;
using CryptoPortfolioTracker.Enums;
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
        ObservableCollection<Coin> GetTopWinners();
        ObservableCollection<Coin> GetTopLosers();
        double GetPortfolioValue();
        double GetCostBase();
        Task<List<CapitalFlowPoint>> GetYearlyMutationsByTransactionKind(TransactionKind transactionKind);
    }
}
