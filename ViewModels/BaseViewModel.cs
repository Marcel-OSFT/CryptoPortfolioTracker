using Microsoft.UI.Dispatching;
using System;
using System.ComponentModel;
using System.Diagnostics;

namespace CryptoPortfolioTracker.ViewModels
{
    public abstract class BaseViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string propertyName)
        {
            VerifyPropertyName(propertyName);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        [Conditional("DEBUG")]
        private void VerifyPropertyName(string propertyName)
        {
            if (TypeDescriptor.GetProperties(this)[propertyName] == null)
                throw new ArgumentNullException(GetType().Name + " does not contain property: " + propertyName);
        }

        ////******* EventHandlers
        //public event PropertyChangedEventHandler PropertyChanged;
        //public void OnPropertyChanged(string name)
        //{
        //    if (MainPage.Current == null) return;
        //    MainPage.Current.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
        //    {
        //        PropertyChangedEventHandler handler = PropertyChanged;
        //        if (handler != null)
        //        {
        //            handler(this, new PropertyChangedEventArgs(name));
        //        }
        //    });
        //}


    }
}