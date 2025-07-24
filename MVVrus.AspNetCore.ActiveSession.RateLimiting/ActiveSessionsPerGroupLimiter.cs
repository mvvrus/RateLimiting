using System.Diagnostics.CodeAnalysis;
using System.Threading.RateLimiting;

namespace MVVrus.AspNetCore.ActiveSession.RateLimiting
{
    public class ActiveSessionsPerGroupLimiter : PartitionedRateLimiter<HttpContext>
    {
        PartitionedRateLimiter<HttpContext> _newActiveSessionLimiter;

        public ActiveSessionsPerGroupLimiter()
        {
            _newActiveSessionLimiter = PartitionedRateLimiter.Create<HttpContext, IActiveSession>(Partitioner, Comparer);
        }

        public override RateLimiterStatistics? GetStatistics(HttpContext resource)
        {
            return _newActiveSessionLimiter.GetStatistics(resource);
        }

        protected override ValueTask<RateLimitLease> AcquireAsyncCore(HttpContext resource, Int32 permitCount, CancellationToken cancellationToken)
        {
            throw new NotImplementedException("TODO");
        }

        protected override RateLimitLease AttemptAcquireCore(HttpContext resource, Int32 permitCount)
        {
            throw new NotImplementedException("TODO");
        }

        static RateLimitPartition<IActiveSession> Partitioner(HttpContext context)
        {
            throw new NotImplementedException("TODO");
        }

        class ActiveSessionComparer : IEqualityComparer<IActiveSession>
        {
            public Boolean Equals(IActiveSession? x, IActiveSession? y)
            {
                throw new NotImplementedException("TODO");
            }

            public Int32 GetHashCode([DisallowNull] IActiveSession obj)
            {
                throw new NotImplementedException("TODO"    );
            }
        }

        static ActiveSessionComparer Comparer = new ActiveSessionComparer();
    }
}
