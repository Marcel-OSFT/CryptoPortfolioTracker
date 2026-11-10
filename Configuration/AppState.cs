using CommunityToolkit.Mvvm.ComponentModel;
using CryptoPortfolioTracker.Models;

namespace CryptoPortfolioTracker.Configuration
{
    public interface IAppState
    {
        Portfolio? CurrentPortfolio { get; set; }
        event EventHandler? CurrentPortfolioChanged;

        PortfolioContext? Context { get; set; }
        event EventHandler? ContextChanged;
        UpdateContext? UpdateContext { get; set; }
        event EventHandler? UpdateContextChanged;

    }

    public class AppState : ObservableObject, IAppState
    {
        //*** Portfolio CurrentPortfolio
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

        public event EventHandler? CurrentPortfolioChanged;

        //**** PortfolioService Context
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

        public event EventHandler? ContextChanged;

        //**** PortfolioService UpdateContext
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

        public event EventHandler? UpdateContextChanged;


    }
}