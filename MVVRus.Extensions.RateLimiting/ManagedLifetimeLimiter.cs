using System.Threading.RateLimiting;

namespace MVVRus.Extensions.RateLimiting
{
    public class ManagedLifetimeLimiter : RateLimiter
    {
        RateLimiter? Inner;

        public ManagedLifetimeLimiter(RateLimiter Inner)
        {
            if(Inner is null) throw new ArgumentNullException(nameof(Inner));
            this.Inner= Inner;
        }

        public override TimeSpan? IdleDuration => Volatile.Read(ref Inner) is null?TimeSpan.MaxValue:null;

        public override RateLimiterStatistics? GetStatistics()
        {
            return Volatile.Read(ref Inner)?.GetStatistics()??throw new ObjectDisposedException(nameof(Inner));
        }

        protected override ValueTask<RateLimitLease> AcquireAsyncCore(Int32 permitCount, CancellationToken cancellationToken)
        {
            return Volatile.Read(ref Inner)?.AcquireAsync(permitCount, cancellationToken)??throw new ObjectDisposedException(nameof(Inner));
        }

        protected override RateLimitLease AttemptAcquireCore(Int32 permitCount)
        {
            return Volatile.Read(ref Inner)?.AttemptAcquire(permitCount) ?? throw new ObjectDisposedException(nameof(Inner));
        }

        protected override void Dispose(bool disposing)
        {
            if(disposing) {
                RateLimiter? inner = Interlocked.Exchange(ref Inner, null);
                if(inner!=null) inner.Dispose();
            }       
        }

        protected override async ValueTask DisposeAsyncCore()
        {
            RateLimiter? inner = Interlocked.Exchange(ref Inner, null);
            if(inner!=null) await inner.DisposeAsync();
            await base.DisposeAsyncCore();
        }

    }
}
