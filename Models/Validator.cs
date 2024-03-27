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
using System.Reflection.Metadata.Ecma335;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;


namespace CryptoPortfolioTracker.Models
{
    public class Validator : BaseModel
    { 

        public Validator(int numberOfItemsToValidate, bool directStart)
        {
            RegisterValidationItems(numberOfItemsToValidate, directStart); 
            IsAllEntriesValid = false;
        }

        CancellationTokenSource tokenSource2 = new CancellationTokenSource();

        
        public ObservableCollection<bool> IsValidEntryCollection { get; set; } = new ObservableCollection<bool>();

        private bool isAllEntriesValid;
        public bool IsAllEntriesValid
        {
            get { return isAllEntriesValid; }
            set
            {
                if (value == isAllEntriesValid) return;
                isAllEntriesValid = value;
                OnPropertyChanged(nameof(IsAllEntriesValid));
            }
        }


        private int nrOfValidEntriesNeeded;
        public int NrOfValidEntriesNeeded
        {
            get { return nrOfValidEntriesNeeded; }
            set
            {
                if (value != nrOfValidEntriesNeeded)
                {
                    nrOfValidEntriesNeeded = value;
                    OnPropertyChanged(nameof(NrOfValidEntriesNeeded));
                }
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
                    IsAllEntriesValid = invalidEntries <= (IsValidEntryCollection.Count-NrOfValidEntriesNeeded) ? true : false;

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

    }
}
