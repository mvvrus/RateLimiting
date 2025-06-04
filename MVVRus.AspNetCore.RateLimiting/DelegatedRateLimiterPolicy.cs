using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace MVVRus.AspNetCore.RateLimiting
{
    public class DelegatedRateLimiterPolicy<TPartitionKey> : IRateLimiterPolicy<TPartitionKey>
    {
        Func<HttpContext, RateLimitPartition<TPartitionKey>> _partitioner;
        public Func<OnRejectedContext, CancellationToken, ValueTask>? OnRejected { get; init; }

        public DelegatedRateLimiterPolicy(Func<HttpContext, RateLimitPartition<TPartitionKey>> partitioner, 
            Func<OnRejectedContext, CancellationToken, ValueTask>? onRejected = null)
        {
            _partitioner = partitioner;
            OnRejected=onRejected;
        }

        public RateLimitPartition<TPartitionKey> GetPartition(HttpContext httpContext)
        {
            return _partitioner(httpContext);
        }
    }
}
