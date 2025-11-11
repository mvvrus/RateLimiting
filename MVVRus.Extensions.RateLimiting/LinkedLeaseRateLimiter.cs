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
        PartitionedRateLimiter<TResource> _baseLimiter;

        protected abstract IShareableLeaseOwner<TResource>? GetLeaseStore(TResource resource);

        public LinkedLeaseRateLimiter(PartitionedRateLimiter<TResource> baseLimiter)
        {
            _baseLimiter = baseLimiter;
        }

        public override RateLimiterStatistics? GetStatistics(TResource resource)
        {
            return _baseLimiter.GetStatistics(resource);
        }

        protected override async ValueTask<RateLimitLease> AcquireAsyncCore(TResource resource, Int32 permitCount, CancellationToken cancellationToken)
        {
            IShareableLeaseOwner<TResource>? leaseStore = GetLeaseStore(resource);
            if(leaseStore!=null)
                return await leaseStore.AcquireLeaseAsync(_baseLimiter, resource, permitCount, cancellationToken);
            else return await _baseLimiter.AcquireAsync(resource,permitCount, cancellationToken);
        }

        protected override RateLimitLease AttemptAcquireCore(TResource resource, Int32 permitCount)
        {
            IShareableLeaseOwner<TResource>? leaseStore = GetLeaseStore(resource);
            if(leaseStore!=null)
                return leaseStore.AcquireLease(_baseLimiter, resource, permitCount);
            else return _baseLimiter.AttemptAcquire(resource, permitCount);
        }

    }
}
