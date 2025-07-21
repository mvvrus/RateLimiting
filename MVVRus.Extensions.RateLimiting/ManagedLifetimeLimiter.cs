using System.Threading.RateLimiting;

namespace MVVRus.Extensions.RateLimiting
{
    public class ManagedLifetimeLimiter : RateLimiter
    {
        RateLimiter? _inner;

        public ManagedLifetimeLimiter(RateLimiter Inner)
        {
            _inner=Inner;
        }

        public override TimeSpan? IdleDuration => _inner is null?TimeSpan.MaxValue:null;

        public override RateLimiterStatistics? GetStatistics()
        {
            return _inner?.GetStatistics()??throw new ObjectDisposedException(nameof(_inner));
        }

        protected override ValueTask<RateLimitLease> AcquireAsyncCore(Int32 permitCount, CancellationToken cancellationToken)
        {
            return _inner?.AcquireAsync(permitCount, cancellationToken)??throw new ObjectDisposedException(nameof(_inner));
        }

        protected override RateLimitLease AttemptAcquireCore(Int32 permitCount)
        {
            return _inner?.AttemptAcquire(permitCount) ?? throw new ObjectDisposedException(nameof(_inner));
        }

        protected override void Dispose(bool disposing)
        {
            if(disposing) {
                RateLimiter? inner = Interlocked.Exchange(ref _inner, null);
                if(inner!=null) inner.Dispose();
            }       
        }

        protected override async ValueTask DisposeAsyncCore()
        {
            RateLimiter? inner = Interlocked.Exchange(ref _inner, null);
            if(inner!=null) await inner.DisposeAsync();
            await base.DisposeAsyncCore();
        }

    }
}
