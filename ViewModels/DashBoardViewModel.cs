using CommunityToolkit.Mvvm.ComponentModel;
using CryptoPortfolioTracker.Controls;
using CryptoPortfolioTracker.Services;
using Serilog;
using Serilog.Core;
using System.Threading.Tasks;

namespace CryptoPortfolioTracker.ViewModels;

public partial class DashboardViewModel : BaseViewModel
{
    private readonly IPreferencesService _preferencesService;

    public DashboardViewModel(IPreferencesService preferencesService) : base(preferencesService)
    {
        Logger = Log.Logger.ForContext(Constants.SourceContextPropertyName, typeof(AssetsViewModel).Name.PadRight(22));
        _preferencesService = preferencesService;
    }
    
    public async Task Initialize()
    {

    }
   
    public void Terminate()
    {

    }



}






