using Microsoft.EntityFrameworkCore;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;


namespace CryptoPortfolioTracker.Models
{
    public class Account : BaseModel
    {
        //******* private variables holding values of public Properties
        public static StandardUICommand commandDel = new StandardUICommand();
        public static XamlUICommand commandEdit = new XamlUICommand();

        public Account()
        {           
            CommandDel = InitStandardUICommand(commandDel, StandardUICommandKind.Delete, "Delete Account", "Delete");
            CommandEdit = InitXamlUICommand(commandEdit, "\uE70F", FontWeights.Normal, "Edit Account", "Edit", Windows.System.VirtualKey.I, Windows.System.VirtualKeyModifiers.Control);
        }

        //******* Public Properties
        [Key] public int Id { get; private set; }

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

        private string about;
        public string About
        {
            get { return about; }
            set
            {
                if (value != about)
                {
                    about = value;
                    OnPropertyChanged(nameof(About));
                }
            }
        }



        //*** Navigation property

        private bool isHoldingAsset;
        [NotMapped]
        public bool IsHoldingAsset
        {
            get { return isHoldingAsset; }
            set
            {
                if (value != isHoldingAsset)
                {
                    isHoldingAsset = value;
                    OnPropertyChanged(nameof(IsHoldingAsset));
                }
            }
        }

        private ICollection<Asset> assets;
        public ICollection<Asset> Assets
        {
            get { return assets; }
            set
            {
                if (value != assets)
                {
                    assets = value;
                    OnPropertyChanged(nameof(Assets));
                }
            }
        }


        private double totalValue;
        [NotMapped]
        public double TotalValue
        {
            get { return totalValue; }
            set
            {
                if (value != totalValue)
                {
                    totalValue = value;
                    OnPropertyChanged(nameof(TotalValue));
                }
            }
        }



        [NotMapped] public ICommand CommandDel { get; set; }
        [NotMapped] public ICommand CommandEdit { get; set; }


        public void CalculateTotalValue()
        {
            if (assets.Count > 0)
            {
                TotalValue = assets.Sum(x => x.Qty * x.Coin.Price);
            }
            else { TotalValue = 0; }
        }


    }
}
