using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.RateLimiting;
using System.Threading.Tasks;

namespace MVVRus.Extensions.RateLimiting
{
    public abstract class LinkedLeaseRateLimiter<TResource, TPartitionKey> : PartitionedRateLimiter<TResource>, IRawRateLimiter<TResource> where TPartitionKey:notnull
    {
        PartitionedRateLimiter<TResource> _baseLimiter;

        protected readonly Func<TResource, TPartitionKey> _keyExtractor;
        protected abstract IShareableLeaseOwner<TResource>? GetLeaseStore(TResource resource);

        public LinkedLeaseRateLimiter(Func<TResource, TPartitionKey> keyExtractor,
            Func<TPartitionKey, RateLimitPartition<TPartitionKey>> partitionMaker, 
            IEqualityComparer<TPartitionKey>? equalityComparer = null)

        {
            _keyExtractor = keyExtractor;
            _baseLimiter = PartitionedRateLimiter.Create(MakePartitioner(keyExtractor,partitionMaker),equalityComparer);
        }

        public LinkedLeaseRateLimiter(Func<TResource, TPartitionKey> keyExtractor,
            Func<TPartitionKey, Func<TPartitionKey, RateLimiter>> limiterMaker, 
            IEqualityComparer<TPartitionKey>? equalityComparer = null)

        {
            _keyExtractor = keyExtractor;
            _baseLimiter = PartitionedRateLimiter.Create(MakePartitioner(keyExtractor, limiterMaker), equalityComparer);
        }

        public override RateLimiterStatistics? GetStatistics(TResource resource)
        {
            return _baseLimiter.GetStatistics(resource);
        }

        protected override async ValueTask<RateLimitLease> AcquireAsyncCore(TResource resource, Int32 permitCount, CancellationToken cancellationToken)
        {
            IShareableLeaseOwner<TResource>? leaseStore = GetLeaseStore(resource);
            if(leaseStore!=null)
                return await leaseStore.AcquireLeaseAsync(resource, permitCount, cancellationToken);
            else return await _baseLimiter.AcquireAsync(resource,permitCount, cancellationToken);
        }

        protected override RateLimitLease AttemptAcquireCore(TResource resource, Int32 permitCount)
        {
            IShareableLeaseOwner<TResource>? leaseStore = GetLeaseStore(resource);
            if(leaseStore!=null)
                return leaseStore.AcquireLease(resource, permitCount);
            else return _baseLimiter.AttemptAcquire(resource, permitCount);
        }

        //Raw (base) lease acqusition methods
        ValueTask<RateLimitLease> IRawRateLimiter<TResource>.AcquireAsync(TResource resource, Int32 permitCount, CancellationToken cancellationToken)
        {
            return _baseLimiter.AcquireAsync(resource, permitCount, cancellationToken);
        }

        RateLimitLease IRawRateLimiter<TResource>.AttemptAcquire(TResource resource, Int32 permitCount)
        {
            return _baseLimiter.AttemptAcquire(resource, permitCount);
        }

        static Func<TResource, RateLimitPartition<TPartitionKey>> MakePartitioner(
            Func<TResource,TPartitionKey> keyExtractor, 
            Func<TPartitionKey, RateLimitPartition<TPartitionKey>> partitionMaker)
        {
            return Partitioner;

            RateLimitPartition<TPartitionKey> Partitioner(TResource resource)
            {
                TPartitionKey key = keyExtractor(resource);
                return new RateLimitPartition<TPartitionKey>(key,
                    partKey => partitionMaker(partKey).Factory(partKey));
            }
        }

        static Func<TResource, RateLimitPartition<TPartitionKey>> MakePartitioner(
            Func<TResource, TPartitionKey> keyExtractor,
            Func<TPartitionKey, Func<TPartitionKey,RateLimiter>> limiterMaker)
        {
            return Partitioner;

            RateLimitPartition<TPartitionKey> Partitioner(TResource resource)
            {
                TPartitionKey key = keyExtractor(resource);
                return new RateLimitPartition<TPartitionKey>(key,
                    partKey => limiterMaker(partKey)(partKey));
            }
        }
    }
}
