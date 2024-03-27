//using ABI.Windows.UI;
using CryptoPortfolioTracker.Infrastructure.Response.Coins;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Reflection.Emit;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.UI.ApplicationSettings;
using Windows.UI.Text;

namespace CryptoPortfolioTracker.Models
{
    public partial class Coin : BaseModel
    {

        //******* private variables holding values of public Properties
        private string apiId;
        private string name;
        private string symbol;
        private long rank;
        private string imageUri;
        private double price;
        private double ath;
        private double change52Week;
        private double change1Month;
        private double marketCap;
        private string about;
        private double change24Hr;
        private string note;



        private bool isAsset;

        public static StandardUICommand commandDel = new StandardUICommand();
        public static XamlUICommand commandNote = new XamlUICommand();
        public static XamlUICommand commandAbout = new XamlUICommand();

        //***** Constructor     
        public Coin()
        {
            isAsset = false;
            note = string.Empty;

            CommandDel = InitStandardUICommand(commandDel, StandardUICommandKind.Delete, "Delete from Library", "Delete");
            CommandNote = InitXamlUICommand(commandNote, "\uE70B", FontWeights.Normal, "Add a Note", "Add/Edit Note");
            CommandAbout = InitXamlUICommand(commandAbout, "\uE946", FontWeights.Normal, "Info about coin", "About", Windows.System.VirtualKey.I, Windows.System.VirtualKeyModifiers.Control);
        }

        
        //******* variables with class only scope


        //******* Public Properties

        public int Id { get; set; }
       
        public string ApiId
        {
            get { return apiId; }
            set
            {
                if (apiId == value) { return; }
                apiId = value;
            }
        }
        public string Name
        {
            get { return name; }
            set
            {
                if (name == value) { return; }
                name = value;
            }
        }
        public string Symbol
        {
            get { return symbol.ToUpper(); }
            set
            {
                if (symbol == value) return;
                symbol = value;
                
            }
        }
        public long Rank    
        {
            get { return rank; }
            set
            {
                if (rank == value) return;
                rank = value;
            }
        }
        public string ImageUri
        {
            get { return imageUri; }
            set
            {
                if (imageUri == value) return;
                imageUri = value;

            }
        }
     
        public double Price
        {
            get { return price; }
            set
            {
                if (price == value) return;
                price = value;
                OnPropertyChanged(nameof(Price));
            }
        }
        
        public double Ath
        {
            get { return ath; }
            set
            {
                if (ath == value) return;
                ath = value;
                OnPropertyChanged(nameof(Ath));
            }
        }
        
        public double Change52Week
        {
            get { return change52Week; }
            set
            {
                if (change52Week == value) return;
                change52Week = value;
                OnPropertyChanged(nameof(Change52Week));
            }
        }
        
        public double Change1Month
        {
            get { return change1Month; }
            set
            {
                if (change1Month == value) return;
                change1Month = value;
                OnPropertyChanged(nameof(Change1Month));
            }
        }
        
        public double MarketCap
        {
            get { return marketCap; }
            set
            {
                if (marketCap == value) return;
                marketCap = value;
                OnPropertyChanged(nameof(MarketCap));
            }
        }
        
        public string About
        {
            get { return about; }
            set
            {
                if (about == value) { return; }
                about = value;
            }
        }
        public double Change24Hr
        {
            get { return change24Hr; }
            set
            {
                if (change24Hr == value) return;
                change24Hr = value;
                OnPropertyChanged(nameof(Change24Hr));
            }
        }
        

        public bool IsAsset
        {
            get { return isAsset; }
            set
            {
                if (isAsset == value) { return; }
                isAsset = value;
                OnPropertyChanged(nameof(IsAsset));
            }
        }

        public string Note
        {
            get { return note; }
            set
            {
                if (value != note)
                {
                    note = value;
                    OnPropertyChanged(nameof(Note));
                }
            }
        }


        [NotMapped] public ICommand CommandNote { get; set; }
        [NotMapped] public ICommand CommandDel { get; set; }
        [NotMapped] public ICommand CommandAbout { get; set; }

        //8888 Navigation Property
        public ICollection<Asset> Assets { get; set; }

        //******* Public Methods

    }
}
