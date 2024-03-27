using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryptoPortfolioTracker.Models
{

    public class AssetAccount : BaseModel
    {
        public AssetAccount()
        {
        }

		private string name;
		public string Name
		{
			get { return name; }
			set
			{
				if (value != name)
				{
					name = value;
					OnPropertyChanged(nameof(Name));
				}
			}
		}

		private double qty;
		public double Qty
		{
			get { return qty; }
			set
			{
				if (value != qty)
				{
					qty = value;
					OnPropertyChanged(nameof(Qty));
				}
			}
		}

		private string symbol;
		public string Symbol
		{
			get { return symbol; }
			set
			{
				if (value != symbol)
				{
					symbol = value;
					OnPropertyChanged(nameof(Symbol));
				}
			}
		}

		private int assetId;
		public int AssetId
		{
			get { return assetId; }
			set
			{
				if (value != assetId)
				{
					assetId = value;
					OnPropertyChanged(nameof(AssetId));
				}
			}
		}





	}

}
