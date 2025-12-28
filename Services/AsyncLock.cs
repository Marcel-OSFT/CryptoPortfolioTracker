using System;
using System.Threading;
using System.Threading.Tasks;

namespace TemperatureMonitor.Services
{
    /// <summary>
    /// Simple async-compatible lock that supports timeout and cancellation.
    /// Usage:
    ///   var releaser = await _asyncLock.LockAsync(TimeSpan.FromSeconds(5), ct);
    ///   if (!releaser.IsAcquired) { /* failed to acquire */ }
    ///   using (releaser) { /* protected section */ }
    /// </summary>
    public sealed class AsyncLock : IDisposable
    {
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private bool _disposed;

        public async Task<Releaser> LockAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            bool taken;
            if (timeout.HasValue)
            {
                taken = await _semaphore.WaitAsync(timeout.Value, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                taken = true;
            }

            return taken ? new Releaser(this) : default;
        }

        private void Release() => _semaphore.Release();

        private void ThrowIfDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(AsyncLock));
        }

        public void Dispose()
        {
            if (_disposed) return;
            _semaphore.Dispose();
            _disposed = true;
        }

        public readonly struct Releaser : IDisposable
        {
            private readonly AsyncLock? _toRelease;

            internal Releaser(AsyncLock toRelease) => _toRelease = toRelease;

            public bool IsAcquired => _toRelease is not null;

            public void Dispose() => _toRelease?.Release();
        }
    }
}