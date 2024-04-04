using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Input;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Windows.Input;


namespace CryptoPortfolioTracker.Models
{
    public partial class Account : BaseModel
    {
        
        public Account(string name = "")
        {
            Name = name;
            about = string.Empty;
        }

        //******* Public Properties
        [Key] public int Id { get; private set; }

        [ObservableProperty] string name;
        [ObservableProperty] string about;

        //*** Set To NotMapped in EntityTypeBuilder
        [ObservableProperty] bool isHoldingAsset;

        //*** Set To NotMapped in EntityTypeBuilder
        [ObservableProperty] double totalValue;

        //*** Navigation property
        [ObservableProperty] ICollection<Asset> assets;

        public void CalculateTotalValue()
        {
            if (Assets.Count > 0)
            {
                TotalValue = Assets.Sum(x => x.Qty * x.Coin.Price);
            }
            else { TotalValue = 0; }
        }


    }
}
