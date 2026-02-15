using System.Threading.RateLimiting;

namespace MVVRus.Extensions.RateLimiting
{
    public class ManagedLifetimeLimiter : PartitionRateLimiterDecorator
    {
        public ManagedLifetimeLimiter(RateLimiter Inner):base(Inner) { }

        public override TimeSpan? IdleDuration => IsDisposed ? TimeSpan.MaxValue : null;

    }
}
