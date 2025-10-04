using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MVVRus.Threading.AsyncSynchroPrimitives
{
    public class AsyncCountdownEvent: IDisposable
    {
        Int32 _count;
        AsyncManualResetEvent _manualResetEvent;
        Int32 _disposedSign = 0;

        void CheckDisposed()
        {
            if(Volatile.Read(ref _disposedSign)>0) throw new ObjectDisposedException(GetType().Name);
        }

        Int32 CurrentCount =>Volatile.Read(ref _count);
        Int32 InitialCount { get; }

        Boolean IsSet { 
            get {
                CheckDisposed();
                throw new NotImplementedException("TODO");
            }
        }

        public AsyncCountdownEvent(Int32 initialCount) 
        { 
            _count = InitialCount = initialCount;
            _manualResetEvent = new AsyncManualResetEvent();
        }

        public void Dispose()
        {
            if(Interlocked.Exchange(ref _disposedSign, 1)==0) _manualResetEvent.Dispose();
        }

        public Boolean Signal()
        {
            CheckDisposed();
            if(CurrentCount<=0) throw new InvalidOperationException();
            Int32 curcnt = Interlocked.Decrement(ref _count);
            if(curcnt==0) {
                _manualResetEvent.Set();
                return true;
            }
            else if(curcnt<0) throw new InvalidOperationException();
            else return false;
        }

        public void Reset()
        {
            CheckDisposed();
            Interlocked.Exchange(ref _count, InitialCount);
            _manualResetEvent.Reset();
        }

        public void AddCount()
        {
            CheckDisposed();
            Int32 curcnt = CurrentCount;
            if(IsSet || curcnt<=0 || curcnt>=InitialCount) throw new InvalidOperationException();
            curcnt = Interlocked.Increment(ref _count);
            if(curcnt>=InitialCount) throw new InvalidOperationException();
        }

        public Task WaitAsync()
        {
            CheckDisposed();
            return _manualResetEvent.WaitAsync();
        }

        public Task WaitAsync(CancellationToken cancellationToken)
        {
            CheckDisposed();
            return _manualResetEvent.WaitAsync(cancellationToken);
        }

        public Task<Boolean> WaitAsync(TimeSpan timeout)
        {
            CheckDisposed();
            return _manualResetEvent.WaitAsync(timeout);
        }

        public Task<Boolean> WaitAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            CheckDisposed();
            return _manualResetEvent.WaitAsync(timeout,cancellationToken);
        }

        public Task<Boolean> WaitAsync(Int32 msecsTimeout)
        {
            CheckDisposed();
            return _manualResetEvent.WaitAsync(msecsTimeout);
        }

        public Task<Boolean> WaitAsync(Int32 msecsTimeout, CancellationToken cancellationToken)
        {
            CheckDisposed();
            return _manualResetEvent.WaitAsync(msecsTimeout, cancellationToken);
        }

    }
}
