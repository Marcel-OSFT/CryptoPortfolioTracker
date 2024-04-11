using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Dispatching;

namespace CryptoPortfolioTracker.Extensions
{
    public static class DispatcherQueueExtensions
    {
        public static bool TryEnqueue(this DispatcherQueue dispatcherQueue, Action enqueueingAction, Action<Exception> exceptionAction)
        {
            return dispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    enqueueingAction();
                }
                catch (Exception exception)
                {
                    exceptionAction(exception);
                }
            });
        }
    }
}
