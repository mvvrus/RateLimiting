using System.Threading.RateLimiting;

namespace MVVRus.Extensions.RateLimiting
{
    public class PartitionRateLimiterDecorator: ReplenishingRateLimiter
    {
        RateLimiter? _inner;
        ReplenishingRateLimiter? _replenishingInner;

        public PartitionRateLimiterDecorator(RateLimiter Inner)
        {
            if(Inner is null) throw new ArgumentNullException(nameof(Inner));
            _inner= Inner;
            _replenishingInner = Inner as ReplenishingRateLimiter;

        }

        public override TimeSpan? IdleDuration => Inner.IdleDuration;

        public RateLimiter Inner => Volatile.Read(ref _inner)??throw new ObjectDisposedException(nameof(Inner));

        public Boolean IsDisposed => Volatile.Read(ref _inner)==null;

        public override RateLimiterStatistics? GetStatistics()
        {
            return Inner.GetStatistics();
        }

        protected override ValueTask<RateLimitLease> AcquireAsyncCore(Int32 permitCount, CancellationToken cancellationToken)
        {
            return Inner.AcquireAsync(permitCount, cancellationToken);
        }

        protected override RateLimitLease AttemptAcquireCore(Int32 permitCount)
        {
            return Inner.AttemptAcquire(permitCount);
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

        public override Boolean IsAutoReplenishing
        {
            get
            {
                if(Volatile.Read(ref _inner) == null) throw new ObjectDisposedException(nameof(Inner));
                return _replenishingInner?.IsAutoReplenishing??true;
            }
        }

        public override Boolean TryReplenish()
        {
            if(Volatile.Read(ref _inner) == null) throw new ObjectDisposedException(nameof(Inner));
            return _replenishingInner?.TryReplenish()??false;
        }

        public override TimeSpan ReplenishmentPeriod
        {
            get
            {
                if(Volatile.Read(ref _inner) == null) throw new ObjectDisposedException(nameof(Inner));
                return _replenishingInner?.ReplenishmentPeriod??TimeSpan.MaxValue;
            }
        }

    }
}
