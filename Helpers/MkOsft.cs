
using CryptoPortfolioTracker.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;


namespace CryptoPortfolioTracker.Helpers;

public class MkOsft
{
    /// <summary>
    /// This gives the maximum memory cleanup when removing an observable collection
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list"></param>
    public static ObservableCollection<T> NullObservableCollection<T>(ObservableCollection<T> list) where T : BaseModel
    {
        if (list is not null)
        {
            while (list.Any())
            {
                try
                {
                    //list[0].Dispose();
                   var item = list[0];
                    list.RemoveAt(0);
                    item = null;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    list.RemoveAt(0);
                }
            }
            list = null;
        }
        return list;
    }

    public static List<T> NullList<T>(List<T> list)
    {
        if (list is not null)
        {
            while (list.Any())
            {
                list[0] = default(T);
                list.RemoveAt(0);
            }
            list = null;
        }
        return list;
    }

}
