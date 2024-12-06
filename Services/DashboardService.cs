
using CommunityToolkit.Mvvm.ComponentModel;
using CryptoPortfolioTracker.Infrastructure.Response.Coins;
using CryptoPortfolioTracker.Models;
using Serilog;
using System.Collections.Generic;
using CryptoPortfolioTracker.Enums;
using LiveChartsCore.Defaults;
using Serilog.Core;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CryptoPortfolioTracker.Infrastructure;
using LanguageExt.Common;
using Microsoft.EntityFrameworkCore;


namespace CryptoPortfolioTracker.Services
{

    public partial class DashboardService : IDashboardService
    {
        private static ILogger Logger { get; set; }
        private readonly PortfolioContext coinContext;

        public DashboardService(PortfolioContext portfolioContext)
        {
            Logger = Log.Logger.ForContext(Constants.SourceContextPropertyName, typeof(GraphService).Name.PadRight(22));
            coinContext = portfolioContext; 
        }

        public async Task StorePriceLevelsToContext(Coin coin)
        {
            

        }
        public Coin GetPriceLevelsFromContext(Coin coin)
        {
            return coin;
        }


    }
}
