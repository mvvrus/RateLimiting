

using System.Threading.RateLimiting;

namespace MVVRus.Extensions.RateLimiting
{
    public interface IRawRateLimiter<TResource>
    {
        ValueTask<RateLimitLease> AcquireAsync(TResource resource, Int32 permitCount, CancellationToken cancellationToken);
        RateLimitLease AttemptAcquire(TResource resource, Int32 permitCount);
    }
}
