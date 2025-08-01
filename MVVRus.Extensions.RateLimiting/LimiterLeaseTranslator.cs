using System.Threading.RateLimiting;

namespace MVVRus.Extensions.RateLimiting
{
    public abstract class LimiterLeaseTranslator<TResource> : PartitionedRateLimiter<TResource>
    {
        PartitionedRateLimiter<TResource>? _innerLimiter;

        public LimiterLeaseTranslator(PartitionedRateLimiter<TResource> InnerLimiter)
        {
            _innerLimiter = InnerLimiter;
        }

        protected abstract RateLimitLease TranslateLease(RateLimitLease lease, TResource resource);

        public override RateLimiterStatistics? GetStatistics(TResource resource)
        {
            return _innerLimiter?.GetStatistics(resource) ?? throw new ObjectDisposedException(nameof(LimiterLeaseTranslator<TResource>));
        }

        protected override async ValueTask<RateLimitLease> AcquireAsyncCore(TResource resource, Int32 permitCount, CancellationToken cancellationToken)
        {
            if (_innerLimiter == null) throw new ObjectDisposedException(nameof(LimiterLeaseTranslator<TResource>));
            RateLimitLease lease = await _innerLimiter.AcquireAsync(resource, permitCount, cancellationToken); 
            return TranslateLease(lease, resource);
        }

        protected override RateLimitLease AttemptAcquireCore(TResource resource, Int32 permitCount)
        {
            RateLimitLease lease = _innerLimiter?.AttemptAcquire(resource, permitCount) ?? throw new ObjectDisposedException(nameof(LimiterLeaseTranslator<TResource>));
            return TranslateLease(lease, resource);
        }

        protected override void Dispose(Boolean disposing)
        {
            if(disposing) {
                PartitionedRateLimiter<TResource>? inner_limiter = Interlocked.Exchange(ref _innerLimiter, null);
                inner_limiter?.Dispose();
            }
            base.Dispose(disposing);
        }

        protected override async ValueTask DisposeAsyncCore()
        {
            PartitionedRateLimiter<TResource>? inner_limiter = Interlocked.Exchange(ref _innerLimiter, null);
            if (inner_limiter!=null) await inner_limiter.DisposeAsync();
            await base.DisposeAsyncCore();
        }


    }
}
