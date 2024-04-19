using CommunityToolkit.Mvvm.ComponentModel;

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

