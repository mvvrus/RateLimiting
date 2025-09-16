using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace MVVRus.AspNetCore.RateLimiting
{
    public class SelectiveRateLimiterPolicy : IRateLimiterPolicy<IHttpContextBackLinkFeature>
    {
        PartitionedRateLimiter<HttpContext> _limiter;

        public SelectiveRateLimiterPolicy(
            PartitionedRateLimiter<HttpContext> limiter,
            Func<OnRejectedContext, CancellationToken, ValueTask>? onRejected = null) 
        {
            _limiter = limiter;
            OnRejected=onRejected;
        }

        public Func<OnRejectedContext, CancellationToken, ValueTask>? OnRejected { get; }

        public RateLimitPartition<IHttpContextBackLinkFeature> GetPartition(HttpContext context)
        {
            IHttpContextBackLinkFeature key = context.GetBackLinkFeature()
                ?? throw new InvalidOperationException("ComplexRateLimiterPolicy: no HttpContext backlink.");

            return new RateLimitPartition<IHttpContextBackLinkFeature>(key, Factory);
        }

        RateLimiter Factory (IHttpContextBackLinkFeature key)
        {
            return new SelectiveRateLimiterAdapter(_limiter, key);
            throw new NotImplementedException();
        }
    }
}
