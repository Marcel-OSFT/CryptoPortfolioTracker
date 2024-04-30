using System;
using Microsoft.UI.Dispatching;

namespace CryptoPortfolioTracker.Extensions;

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
