


using System.Runtime.CompilerServices;
using System.Threading.RateLimiting;

namespace MVVRus.Extensions.RateLimiting
{
    public class SingleShareableLeaseOwner<TResource> : IShareableLeaseOwner<TResource>
    {
        IRawRateLimiter<TResource> _rawLimiter;
        ILeaseContainer _container;
        Int32 _disposedValue=0;

        public event EventHandler? DisposedEvent;

        public SingleShareableLeaseOwner(IRawRateLimiter<TResource> rawLimiter, ILeaseContainer container)
        {
            _rawLimiter = rawLimiter;
            _container = container;
        }

        public DerivedLease AcquireLease(TResource resource, Int32 permitCount)
        {
            RateLimitLease? lease;
            Boolean must_dispose = false;
            if(!_container.TryGetLease(out lease)) {
                lease = _rawLimiter.AttemptAcquire(resource, permitCount);
                must_dispose = !TryStoreLease(ref lease);
            }
            DerivedLease result = MakeDerived(lease, permitCount);
            if(must_dispose) lease.Dispose();
            return result;
        }

        public async ValueTask<DerivedLease> AcquireLeaseAsync(TResource resource, Int32 permitCount, CancellationToken cancellationToken)
        {
            RateLimitLease? lease;
            Boolean must_dispose = false;
            if(!_container.TryGetLease(out lease)) {
                lease = await _rawLimiter.AcquireAsync(resource, permitCount,cancellationToken);
                must_dispose = !TryStoreLease(ref lease);
            }
            DerivedLease result = MakeDerived(lease, permitCount);
            if(must_dispose) lease.Dispose();
            return result;
        }

        public void Release(DerivedLease derivedLease)
        {
            //Nothing to do in this class
        }

        private Boolean TryStoreLease(ref RateLimitLease lease)
        //Return true only if the lease has been just stored successfully in the container so we are no more responsible for its cleanup
        {
            return (lease.IsAcquired) ? _container.TrySetLease(ref lease) : false;
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
