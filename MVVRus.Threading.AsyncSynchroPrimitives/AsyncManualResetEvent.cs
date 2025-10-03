namespace MVVRus.Threading.AsyncSynchroPrimitives
{
    public class AsyncManualResetEvent: IDisposable
    {
       
        CancellationTokenSource _disposeCts;
        TaskCompletionSource _signalWaiterTcs;
        Int32 _disposedSign = 0;

        void CheckDisposed()
        {
            if(Volatile.Read(ref _disposedSign)>0) throw new ObjectDisposedException(GetType().Name);
        }

        public AsyncManualResetEvent(): this(false) { }

        public AsyncManualResetEvent(Boolean initialState) 
        {
            _disposeCts = new CancellationTokenSource();
            _signalWaiterTcs = new TaskCompletionSource();
            if(initialState) _signalWaiterTcs.SetResult();
        }

        public Boolean IsSet { 
            get {
                CheckDisposed();
                TaskCompletionSource tcs = Volatile.Read(ref _signalWaiterTcs);
                return tcs.Task.IsCompleted;
            }
        }

        public void Set()
        {
            CheckDisposed();
            _signalWaiterTcs.TrySetResult();
        }

        public void Reset()
        {
            TaskCompletionSource tcs;
            do {
                CheckDisposed();
                tcs = Volatile.Read(ref _signalWaiterTcs);
            } while(tcs.Task.IsCompleted && Interlocked.CompareExchange(ref _signalWaiterTcs,new TaskCompletionSource(),tcs) !=tcs);
        }


        public void Dispose()
        {
            DoDispose();
        }

        void DoDispose()
        {
            if(Interlocked.Exchange(ref _disposedSign,1)==0) {
                _signalWaiterTcs?.TrySetException(new ObjectDisposedException(GetType().Name));
                _disposeCts.Cancel();
                _disposeCts.Dispose();
            }
        }

        public Task WaitAsync()
        {
            return WaitAsync(Timeout.Infinite, CancellationToken.None);
        }

        public Task WaitAsync(CancellationToken cancellationToken)
        {
            return WaitAsync(Timeout.Infinite, cancellationToken);
        }

        public Task<Boolean> WaitAsync(TimeSpan timeout)
        {
            return WaitAsync(ConvertTimeoutToMsecs(timeout));
        }

        public Task<Boolean> WaitAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            return WaitAsync(ConvertTimeoutToMsecs(timeout), cancellationToken);
        }

        public Task<Boolean> WaitAsync(Int32 msecsTimeout)
        {
            return WaitAsync(msecsTimeout, CancellationToken.None);
        }

        public Task<Boolean> WaitAsync(Int32 msecsTimeout, CancellationToken cancellationToken)
        {
            CheckDisposed();
            TaskCompletionSource tcs = Volatile.Read(ref _signalWaiterTcs);
            if(tcs.Task.IsCompleted) {
                if(tcs.Task.IsCompletedSuccessfully) return Task.FromResult(true);
                else if(tcs.Task.IsFaulted) return Task.FromException<Boolean>(tcs.Task.Exception);
                else return Task.FromCanceled<Boolean>(cancellationToken); //Should never happen
            }

            CancellationTokenSource? delay_cts = null;
            CancellationToken delay_token = _disposeCts.Token;
            if(cancellationToken.CanBeCanceled) {
                delay_cts= CancellationTokenSource.CreateLinkedTokenSource(delay_token, cancellationToken);
                delay_token=delay_cts.Token;
            }

            return new ResultTaskClosure(tcs,msecsTimeout,delay_cts, delay_token).ResultTask();


            throw new NotImplementedException();
        }

        Int32 ConvertTimeoutToMsecs(TimeSpan timeout)
        {
            return timeout == Timeout.InfiniteTimeSpan ? Timeout.Infinite: (Int32)timeout.TotalMicroseconds;
        }

        class ResultTaskClosure
        {
            CancellationTokenSource? _delayCts;
            CancellationToken _delayToken ;
            TaskCompletionSource _signalTcs;
            Int32 _msecsTimeout;

            public ResultTaskClosure(
                TaskCompletionSource signalTcs, 
                Int32 msecsTimeout, 
                CancellationTokenSource? delayCts, 
                CancellationToken delayToken) 
            {
                _signalTcs = signalTcs;
                _msecsTimeout = msecsTimeout;
                _delayCts = delayCts;
                _delayToken = delayToken;
            }

            public async Task<Boolean> ResultTask() 
            {
                Task awaited_task = await Task.WhenAny(_signalTcs.Task, Task.Delay(_msecsTimeout, _delayToken)).ConfigureAwait(false);
                _delayCts?.Dispose();
                if(_signalTcs.Task.IsCompleted) awaited_task = _signalTcs.Task;
                await awaited_task.ConfigureAwait(false); //To re-raise an exception from the task if any
                return _signalTcs.Task.IsCompletedSuccessfully;
            }
        }

    }
}
