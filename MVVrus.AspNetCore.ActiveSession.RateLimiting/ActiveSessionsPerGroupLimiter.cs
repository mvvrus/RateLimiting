using System.Diagnostics.CodeAnalysis;
using System.Threading.RateLimiting;
using MVVRus.Extensions.RateLimiting;

namespace MVVrus.AspNetCore.ActiveSession.RateLimiting
{
    public class ActiveSessionsPerGroupLimiter : PartitionedRateLimiter<HttpContext>
    {
        PartitionedRateLimiter<HttpContext> _newActiveSessionLimiter;
        ConcurrencyLimiterOptions _options;

        //Func<ILocalSession, RateLimitPartition<ILocalSession>> _func;
        //TODO = ManagedLifetimePartition.GetManagedLifetimeLimiter<ILocalSession>(partitionFactory: inner);

        public ActiveSessionsPerGroupLimiter(ConcurrencyLimiterOptions options)
        {
            _options = options;
            Func<HttpContext, RateLimitPartition<ILocalSession>> partitioner = Partitioner;
            _newActiveSessionLimiter = PartitionedRateLimiter.Create(
                partitioner, 
                Comparer);
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

        protected override void Dispose(Boolean disposing)
        {
            if(disposing) _newActiveSessionLimiter.Dispose();
            base.Dispose(disposing);
        }

        protected override async ValueTask DisposeAsyncCore()
        {
            await _newActiveSessionLimiter.DisposeAsync();
            await base.DisposeAsyncCore();
        }


        RateLimitPartition<ILocalSession> Partitioner(HttpContext context)
        {   
            ILocalSession local_session = context.GetLocalSession();
            return ManagedLifetimePartition.GetManagedLifetimeLimiter(
                local_session,
                key=>RateLimitPartition.GetConcurrencyLimiter(key, _=>_options),
                Registrar);
        }

        void Registrar(ManagedLifetimeLimiter limiter, ILocalSession sessionGroup, Object? _)
        {
            throw new NotImplementedException("TODO");
        }

        class SessionGroupComparer : IEqualityComparer<ILocalSession>
        {
            public Boolean Equals(ILocalSession? x, ILocalSession? y)
            {
                if(x is null) return (y is null);
                else return x.Id.Equals(y?.Id);
            }

            public Int32 GetHashCode([DisallowNull] ILocalSession obj)
            {
                return obj.GetType().GetHashCode() ^ obj.Id.GetHashCode();
            }
        }

        static SessionGroupComparer Comparer = new SessionGroupComparer();

        class SurrogateLease : RateLimitLease
        {

            public override Boolean IsAcquired => true;

            public override IEnumerable<String> MetadataNames => Array.Empty<String>();

            public override Boolean TryGetMetadata(String metadataName, out Object? metadata)
            {
                metadata = null;
                return false;
            }
        }

        static SurrogateLease SuccessLease = new SurrogateLease();
    }
}
