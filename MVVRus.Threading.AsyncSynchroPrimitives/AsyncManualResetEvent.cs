namespace MVVRus.Threading.AsyncSynchroPrimitives
{
    public class AsyncManualResetEvent: IDisposable
    {
       
        CancellationTokenSource _disposeCts;
        TaskCompletionSource _signalWaiterTcs;

        public AsyncManualResetEvent() 
        {
            _disposeCts = new CancellationTokenSource();
            _signalWaiterTcs = new TaskCompletionSource();
        }

        public Boolean IsSet { 
            get {
                TaskCompletionSource tcs = Volatile.Read(ref _signalWaiterTcs);
                return tcs!=null && tcs.Task.IsCompleted;
            }
        }

        public void Set()
        {
            _signalWaiterTcs.TrySetResult();
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }


        public void Dispose()
        {
            DoDispose();
            //TODO
        }

        void DoDispose()
        {
            _signalWaiterTcs?.TrySetException(new ObjectDisposedException(GetType().Name));
            _disposeCts.Cancel();
            _disposeCts.Dispose();
            //TODO
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
                return _signalTcs.Task.IsCompletedSuccessfully;
            }
        }

    }
}
