


using System.Runtime.CompilerServices;
using System.Threading.RateLimiting;

namespace MVVRus.Extensions.RateLimiting
{
    public abstract class SingleShareableLeaseOwner<TResource> : IShareableLeaseOwner<TResource>
    {
        Int32 _disposedValue=0;
        Task<RateLimitLease>? _rawLeaseTask=null;
        protected abstract Boolean TryGetLease(out RateLimitLease? lease);
        protected abstract Boolean TrySetLease(ref RateLimitLease lease);

        public event EventHandler? DisposedEvent;

        public DerivedLease AcquireLease(PartitionedRateLimiter<TResource> baseLimiter, TResource resource, Int32 permitCount)
        {
            RateLimitLease? lease, acquired_lease=null;
            Boolean must_dispose_lease = false;
            if(!TryGetLease(out lease)) {
                lease = acquired_lease = baseLimiter.AttemptAcquire(resource, 1);
                must_dispose_lease = !TrySetLease(ref lease);
            }
            DerivedLease result = MakeDerived(lease!, permitCount);
            if(must_dispose_lease) acquired_lease?.Dispose();
            return result;
        }

        public async ValueTask<DerivedLease> AcquireLeaseAsync(PartitionedRateLimiter<TResource> baseLimiter, TResource resource, Int32 permitCount, CancellationToken cancellationToken)
        {
            RateLimitLease? lease, acquired_lease = null;
            Boolean must_dispose_lease = false;
            if(!TryGetLease(out lease)) {
                Task<RateLimitLease>? current_lease_task;
                //No raw (i.e. base) lease yet. Try to acquire it async
                while((current_lease_task=Volatile.Read(ref _rawLeaseTask)) == null) {
                    TaskCompletionSource start_tcs = new TaskCompletionSource(); //Used to delay the raw lease acquisition task
                    Task<RateLimitLease> new_raw_lease_task = start_tcs.Task.ContinueWith(
                            task => baseLimiter.AcquireAsync(resource, 1, cancellationToken).AsTask(),
                            cancellationToken,
                            TaskContinuationOptions.OnlyOnRanToCompletion,
                            TaskScheduler.Default
                        ).Unwrap();
                    current_lease_task = Interlocked.CompareExchange(ref _rawLeaseTask, new_raw_lease_task, null);
                    if(current_lease_task!=null)
                        // _rawLeaseTask has been already set while we creating new_raw_lease_task
                        start_tcs.SetCanceled();
                    else 
                        // Now we can allow raw lease acquisition task to be performed
                        start_tcs.TrySetResult();          
                }
                lease = acquired_lease = await current_lease_task;
                must_dispose_lease = !TrySetLease(ref lease);
                RateLimitLease? placeholder;
                Task? abandoned;
                if(!lease.IsAcquired && !TryGetLease(out placeholder)) 
                    abandoned = Interlocked.CompareExchange(ref _rawLeaseTask, null, current_lease_task); //Plan to acquire a permissive lease ones more
            }
            DerivedLease result = MakeDerived(lease!, permitCount);
            if(must_dispose_lease) acquired_lease?.Dispose();
            return result;
        }

        public void Release(DerivedLease derivedLease)
        {
            //Nothing to do in this class
        }

        protected virtual DerivedLease MakeDerived(RateLimitLease lease, Int32 permitCount)
        {
            return new DerivedLease(this, lease.IsAcquired, permitCount,lease.GetAllMetadata().ToDictionary());
        }

        protected virtual void Dispose(Boolean disposing)
        {
            if(disposing) {
                EventHandler? t = Volatile.Read(ref DisposedEvent);
                FireEvent(t);
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Int32 disposedValue = Interlocked.Exchange(ref _disposedValue, 1);
            if(disposedValue>0) {
                Dispose(disposing: true);
                GC.SuppressFinalize(this);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        void FireEvent(EventHandler? handler)
        {
            handler?.Invoke(this, EventArgs.Empty);
        }
    }
}
