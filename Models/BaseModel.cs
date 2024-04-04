using CryptoPortfolioTracker.Infrastructure.Response.Coins;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
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
using System.Runtime.CompilerServices;

namespace CryptoPortfolioTracker.Models
{
    public partial class BaseModel : ObservableObject
    {

        //***** Constructor     
        public BaseModel()
        {
           
        }

        ////******* EventHandlers
        //public event PropertyChangedEventHandler PropertyChanged;
        //protected void OnPropertyChanged([CallerMemberName] string name = null)
        //{
        //    if (MainPage.Current == null) return;
        //    MainPage.Current.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
        //    {
        //        //Debug.WriteLine("OnPropertyChanged (BaseModel) => " + name);

        //        PropertyChangedEventHandler handler = PropertyChanged;
        //        if (handler != null)
        //        {
        //            handler(this, new PropertyChangedEventArgs(name));
        //        }
        //    });
        //}

        //public event PropertyChangedEventHandler? PropertyChanged;

        //protected void OnPropertyChanged(string propertyName)
        //{
        //    VerifyPropertyName(propertyName);
        //    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        //}

        //[Conditional("DEBUG")]
        //private void VerifyPropertyName(string propertyName)
        //{
        //    if (TypeDescriptor.GetProperties(this)[propertyName] == null)
        //        throw new ArgumentNullException(GetType().Name + " does not contain property: " + propertyName);
        //}
    }
}

