using System.Threading.RateLimiting;

namespace MVVRus.Extensions.RateLimiting
{
    public class ManagedLifetimeLimiter : RateLimiter, IRateLimiterAggregator
    {
        //The code from this class is intentionally copypasted into the ManagedLifetimeReplenishingLimiter class
        //It must be made so due to absence of multiple inheritance in the C# language
        //After making corrections to the code don't forget to copy those corrections into ManagedLifetimeReplenishingLimiter.cs file

        RateLimiter? _inner;

        public ManagedLifetimeLimiter(RateLimiter Inner)
        {
            if(Inner is null) throw new ArgumentNullException(nameof(Inner));
            if(Inner is ReplenishingRateLimiter)
                throw new InvalidOperationException($"An instance of this class may not be based on the {Inner.GetType().Name} class because it is a descendant of a ReplenishingLimiter class. Use ManagedLifetimeReplenishingLimiter class instead.");
            _inner= Inner;
        }

        public override TimeSpan? IdleDuration => Volatile.Read(ref _inner) is null?TimeSpan.MaxValue:null;

        public RateLimiter Inner => Volatile.Read(ref _inner)??throw new ObjectDisposedException(nameof(Inner));

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

    }
}
