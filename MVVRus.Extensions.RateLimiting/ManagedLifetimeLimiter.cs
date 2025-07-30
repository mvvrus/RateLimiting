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

        public override TimeSpan? IdleDuration => Inner is null?TimeSpan.MaxValue:null;

        public override RateLimiterStatistics? GetStatistics()
        {
            return Inner?.GetStatistics()??throw new ObjectDisposedException(nameof(Inner));
        }

        protected override ValueTask<RateLimitLease> AcquireAsyncCore(Int32 permitCount, CancellationToken cancellationToken)
        {
            return Inner?.AcquireAsync(permitCount, cancellationToken)??throw new ObjectDisposedException(nameof(Inner));
        }

        protected override RateLimitLease AttemptAcquireCore(Int32 permitCount)
        {
            return Inner?.AttemptAcquire(permitCount) ?? throw new ObjectDisposedException(nameof(Inner));
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
