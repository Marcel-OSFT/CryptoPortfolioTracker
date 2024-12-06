using CommunityToolkit.Mvvm.ComponentModel;
using CryptoPortfolioTracker.Controls;
using CryptoPortfolioTracker.Services;
using Serilog;
using Serilog.Core;
using System.ComponentModel;

namespace CryptoPortfolioTracker.ViewModels
{

    public sealed partial class PriceLevelsViewModel : BaseViewModel, INotifyPropertyChanged
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public static PriceLevelsViewModel Current;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        private readonly IPreferencesService _preferencesService;


        public PriceLevelsViewModel(IPreferencesService preferencesService) : base(preferencesService)
        {
            Logger = Log.Logger.ForContext(Constants.SourceContextPropertyName, typeof(AccountsViewModel).Name.PadRight(22));
            Current = this;
            _preferencesService = preferencesService;
        }


    }
}
