using System.Threading.RateLimiting;

namespace MVVRus.Extensions.RateLimiting
{
    internal class ManagedLifetimeReplenishingLimiter: ReplenishingRateLimiter, IRateLimiterAggregator
    {
        //Most of a code of this class is copypasted from ManagedLifetimeLimiter.class due to lack of multiple inheritance in C#.
        //Don't forget to keep that code in sync.

        ReplenishingRateLimiter? _inner;

        public ManagedLifetimeReplenishingLimiter(ReplenishingRateLimiter Inner)
        {
            if(Inner is null) throw new ArgumentNullException(nameof(Inner));
            this._inner= Inner;
        }

        public override TimeSpan? IdleDuration => Volatile.Read(ref _inner) is null ? TimeSpan.MaxValue : null;

        public RateLimiter Inner => _inner??throw new ObjectDisposedException(nameof(Inner));

        public override RateLimiterStatistics? GetStatistics()
        {
            return Volatile.Read(ref _inner)?.GetStatistics()??throw new ObjectDisposedException(nameof(Inner));
        }

        protected override ValueTask<RateLimitLease> AcquireAsyncCore(Int32 permitCount, CancellationToken cancellationToken)
        {
            return Volatile.Read(ref _inner)?.AcquireAsync(permitCount, cancellationToken)??throw new ObjectDisposedException(nameof(Inner));
        }

        protected override RateLimitLease AttemptAcquireCore(Int32 permitCount)
        {
            return Volatile.Read(ref _inner)?.AttemptAcquire(permitCount) ?? throw new ObjectDisposedException(nameof(Inner));
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

        public override Boolean IsAutoReplenishing => _inner?.IsAutoReplenishing??throw new ObjectDisposedException(nameof(Inner));

        public override Boolean TryReplenish()
        {
            return _inner?.TryReplenish()??throw new ObjectDisposedException(nameof(Inner));
        }

        public override TimeSpan ReplenishmentPeriod => _inner?.ReplenishmentPeriod??throw new ObjectDisposedException(nameof(Inner));

    }
}
