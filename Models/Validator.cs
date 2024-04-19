using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CryptoPortfolioTracker.Extensions;
using Microsoft.UI.Dispatching;


namespace CryptoPortfolioTracker.Models
{
    public partial class Validator : BaseModel, INotifyPropertyChanged
    {
        CancellationTokenSource tokenSource2 = new CancellationTokenSource();
        private readonly DispatcherQueue dispatcherQueue;

        public Validator(int numberOfItemsToValidate, bool directStart)
        {
            this.dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            IsValidEntryCollection = new ObservableCollection<bool>();
            ; RegisterValidationItems(numberOfItemsToValidate, directStart);
            IsAllEntriesValid = false;
        }

        [ObservableProperty]
        private ObservableCollection<bool> isValidEntryCollection;
        [ObservableProperty] private int nrOfValidEntriesNeeded;


        private bool isAllEntriesValid;
        public bool IsAllEntriesValid
        {
            get
            {
                return isAllEntriesValid;
            }
            set
            {
                if (value == isAllEntriesValid) return;
                isAllEntriesValid = value;
                OnPropertyChanged();
            }
        }

        public void Start()
        {
            ValidateEntriesAsync();
        }
        public void Stop()
        {
            tokenSource2.Cancel();
            tokenSource2.Dispose();
        }

        private void RegisterValidationItems(int numberOfPropertiesToValidate, bool directStart)
        {
            //** Bind 'IsValid..' properties with Validator.IsValidEntry[x] in your XAML
            //88 example:   IsEntryMatched="{Binding Validator.IsValidEntry[0], Mode=TwoWay}"

            for (var i = 1; i <= numberOfPropertiesToValidate; i++)
            {
                IsValidEntryCollection.Add(false);
            }
            if (directStart) ValidateEntriesAsync();
        }

        private Task ValidateEntriesAsync()
        {
            CancellationToken ct = tokenSource2.Token;

            var task = Task.Run(async () =>
            {
                // Were we already canceled?
                ct.ThrowIfCancellationRequested();

                bool moreToDo = true;
                while (moreToDo)
                {
                    int invalidEntries = IsValidEntryCollection.Where(x => x == false).Count();
                    IsAllEntriesValid = invalidEntries <= (IsValidEntryCollection.Count - NrOfValidEntriesNeeded) ? true : false;

                    await Task.Delay(500);
                    // Debug.WriteLine("Validate...");
                    // Poll on this property if you have to do
                    // other cleanup before throwing.
                    if (ct.IsCancellationRequested)
                    {
                        // Clean up here, then...
                        ct.ThrowIfCancellationRequested();
                    }
                }
            }, tokenSource2.Token); // Pass same token to Task.Run.
            return task;
        }


        //******* EventHandlers
        //public event PropertyChangedEventHandler PropertyChanged;
        //protected void OnPropertyChanged([CallerMemberName] string name = null)
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

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            this.dispatcherQueue.TryEnqueue(() =>
            {
                PropertyChangedEventHandler handler = PropertyChanged;
                if (handler != null)
                {
                    handler(this, new PropertyChangedEventArgs(name));
                }
            },
            exception =>
            {
                throw new Exception(exception.Message);
            });
        }


    }
}
