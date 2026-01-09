
using System.Threading.RateLimiting;

namespace MVVRus.Extensions.RateLimiting
{
    public interface IRateLimiterAggregator
    {
        RateLimiter Inner { get; }
    }
}
