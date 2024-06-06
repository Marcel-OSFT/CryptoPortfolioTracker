
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CryptoPortfolioTracker.Helpers
{
    public class MkOsft
    {
        /// <summary>
        /// This gives the maximum memory cleanup when removing an observable collection
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        public static void ClearObservableCollection<T>(ObservableCollection<T> list)
        {
            if (list is not null)
            {
                for (var i = 0; i < list.Count; i++)
                {
                    list[i] = default(T);
                    list.RemoveAt(i);
                }
                list = null;
            }
        }
        public static void ClearList<T>(List<T> list)
        {
            if (list is not null)
            {
                for (var i = 0; i < list.Count; i++)
                {
                    list[i] = default(T);
                    list.RemoveAt(i);
                }
                list = null;
            }
        }
    }
}
