using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;


namespace CryptoPortfolioTracker.Models
{
    public partial class Transaction : BaseModel
    {
        public Transaction()
        {
            Mutations = new Collection<Mutation>();
        }

        [ObservableProperty] int id;
        [ObservableProperty] DateTime timeStamp;
        [ObservableProperty] string note = string.Empty;
        
        //******* Navigation Properties
        [ObservableProperty] ICollection<Mutation> mutations;

    }
}
