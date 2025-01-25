
using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace CryptoPortfolioTracker.Models
{
    public partial class Portfolio : ObservableObject
    {
        public string Name { get; set; } = string.Empty;
        public string Signature { get; set; } = string.Empty; // -> //Portfolios/PortfolioGuid

        [ObservableProperty] private DateTime lastAccess = DateTime.MinValue;

        
    }
}
