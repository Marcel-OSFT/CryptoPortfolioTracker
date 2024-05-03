using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CryptoPortfolioTracker.Extensions;
using Microsoft.UI.Dispatching;


namespace CryptoPortfolioTracker.Models;

public partial class Validator : BaseModel, INotifyPropertyChanged
{
    private readonly CancellationTokenSource tokenSource2 = new();
    private readonly DispatcherQueue dispatcherQueue;

    public Validator(int numberOfItemsToValidate, bool directStart)
    {
        dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        EntryCollection = new ObservableCollection<bool>();
        RegisterValidationItems(numberOfItemsToValidate, directStart);
        IsAllEntriesValid = false;
    }

    [ObservableProperty] private ObservableCollection<bool> entryCollection;
    private List<int>? entriesToValidate;

    public void RegisterEntriesToValidate(int[] index)
    {
        entriesToValidate = new List<int>();
        foreach (var i in index)
        {
            entriesToValidate.Add(i);
        }
    }
    public void UnRegisterEntriesToValidate(int[] index)
    {
        if (entriesToValidate is null) return;
        foreach (var i in index)
        {
            entriesToValidate.Remove(i);
        }
    }

    private bool isAllEntriesValid;
    public bool IsAllEntriesValid
    {
        get => isAllEntriesValid;
        set
        {
            if (value == isAllEntriesValid)
            {
                return;
            }
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
            EntryCollection.Add(false);
        }
        if (directStart)
        {
            ValidateEntriesAsync();
        }
    }

    private Task ValidateEntriesAsync()
    {
        
        var ct = tokenSource2.Token;

        var task = Task.Run(async () =>
        {
            // Were we already canceled?
            ct.ThrowIfCancellationRequested();

            var moreToDo = true;
            while (moreToDo)
            {
                var invalidEntries = 0;
               
                if (entriesToValidate is not null)
                {
                    foreach (var entry in entriesToValidate)
                    {
                        invalidEntries = EntryCollection.ElementAt(entry) == false 
                            ? invalidEntries + 1 
                            : invalidEntries;
                    }
                }
                IsAllEntriesValid = invalidEntries == 0;

                await Task.Delay(500);
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


    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        dispatcherQueue.TryEnqueue(() =>
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        },
        exception =>
        {
            throw new Exception(exception.Message);
        });
    }


}
