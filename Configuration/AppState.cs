using CommunityToolkit.Mvvm.ComponentModel;
using CryptoPortfolioTracker.Models;

namespace CryptoPortfolioTracker.Configuration
{
    public interface IAppState
    {
        Portfolio? CurrentPortfolio { get; set; }
        PortfolioContext? Context { get; set; }
        UpdateContext? UpdateContext { get; set; }
        string? Currency { get; set; }

        event EventHandler? CurrentPortfolioChanged;
        event EventHandler? ContextChanged;
        event EventHandler? UpdateContextChanged;
        event EventHandler? CurrencyChanged;
    }

    public class AppState : ObservableObject, IAppState
    {
        //*** AppSettings Currency
        public event EventHandler? CurrencyChanged;
        private string? _currency;
        public string? Currency
        {
            get => _currency;
            set
            {
                if (_currency != value)
                {
                    _currency = value;
                    OnPropertyChanged();
                    CurrencyChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }





        //*** Portfolio CurrentPortfolio
        public event EventHandler? CurrentPortfolioChanged;
        private Portfolio? _currentPortfolio;
        public Portfolio? CurrentPortfolio
        {
            get => _currentPortfolio;
            set
            {
                if (_currentPortfolio != value)
                {
                    _currentPortfolio = value;
                    OnPropertyChanged();
                    CurrentPortfolioChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }


        //**** PortfolioService Context
        public event EventHandler? ContextChanged;
        private PortfolioContext? _context;
        public PortfolioContext? Context
        {
            get => _context;
            set
            {
                if (_context != value)
                {
                    _context = value;
                    OnPropertyChanged();
                    ContextChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }


        //**** PortfolioService UpdateContext
        public event EventHandler? UpdateContextChanged;
        private UpdateContext? _updateContext;
        public UpdateContext? UpdateContext
        {
            get => _updateContext;
            set
            {
                if (_updateContext != value)
                {
                    _updateContext = value;
                    OnPropertyChanged();
                    UpdateContextChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }



    }
}