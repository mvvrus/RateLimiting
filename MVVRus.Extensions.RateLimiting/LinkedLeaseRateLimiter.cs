using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.RateLimiting;
using System.Threading.Tasks;

namespace MVVRus.Extensions.RateLimiting
{
    public abstract class LinkedLeaseRateLimiter<TResource, TPartitionKey> : PartitionedRateLimiter<TResource> where TPartitionKey:notnull
    {
        PartitionedRateLimiter<TResource>? _baseLimiter;
        Boolean _disposeBaseLimiter=false;

        protected abstract IShareableLeaseOwner<TResource>? GetLeaseStore(TResource resource);

        public LinkedLeaseRateLimiter(PartitionedRateLimiter<TResource> baseLimiter)
        {
            _baseLimiter = baseLimiter;
        }

        protected LinkedLeaseRateLimiter(PartitionedRateLimiter<TResource> baseLimiter, Boolean disposeBaseLimiter)
        {
            _baseLimiter = baseLimiter;
            _disposeBaseLimiter = disposeBaseLimiter;
        }

        public override RateLimiterStatistics? GetStatistics(TResource resource)
        {
            return GetBaseLimiterThrowIfDisposed().GetStatistics(resource);
        }

        protected override async ValueTask<RateLimitLease> AcquireAsyncCore(TResource resource, Int32 permitCount, CancellationToken cancellationToken)
        {
            PartitionedRateLimiter<TResource> base_limiter = GetBaseLimiterThrowIfDisposed();
            IShareableLeaseOwner<TResource>? leaseStore = GetLeaseStore(resource);
            if(leaseStore!=null)
                return await leaseStore.AcquireLeaseAsync(base_limiter, resource, permitCount, cancellationToken);
            else return await base_limiter.AcquireAsync(resource,permitCount, cancellationToken);
        }

        protected override RateLimitLease AttemptAcquireCore(TResource resource, Int32 permitCount)
        {
            PartitionedRateLimiter<TResource> base_limiter = GetBaseLimiterThrowIfDisposed();
            IShareableLeaseOwner<TResource>? leaseStore = GetLeaseStore(resource);
            if(leaseStore!=null)
                return leaseStore.AcquireLease(base_limiter, resource, permitCount);
            else return base_limiter.AttemptAcquire(resource, permitCount);
        }

        protected override void Dispose(Boolean disposing)
        {
            PartitionedRateLimiter<TResource>? base_limiter = Interlocked.Exchange(ref _baseLimiter, null);
            if(disposing && _disposeBaseLimiter) if(base_limiter!=null) base_limiter.Dispose();
            base.Dispose(disposing);
        }

        protected async override ValueTask DisposeAsyncCore()
        {
            PartitionedRateLimiter<TResource>? base_limiter = Interlocked.Exchange(ref _baseLimiter, null);
            if(_disposeBaseLimiter && base_limiter!=null) await base_limiter.DisposeAsync();
            await base.DisposeAsyncCore();
        }

        private PartitionedRateLimiter<TResource> GetBaseLimiterThrowIfDisposed()
        {
            PartitionedRateLimiter<TResource>? base_limiter = Volatile.Read(ref _baseLimiter);
            if(base_limiter==null) throw new ObjectDisposedException(nameof(LinkedLeaseRateLimiter<TResource,TPartitionKey>));
            return base_limiter;
        }

    }
}
