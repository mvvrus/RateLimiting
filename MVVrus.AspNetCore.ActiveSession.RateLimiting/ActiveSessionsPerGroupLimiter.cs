using System.Diagnostics.CodeAnalysis;
using System.Threading.RateLimiting;
using MVVRus.Extensions.RateLimiting;

namespace MVVrus.AspNetCore.ActiveSession.RateLimiting
{
    public class ActiveSessionsPerGroupLimiter : PartitionedRateLimiter<HttpContext>
    {
        PartitionedRateLimiter<HttpContext> _newActiveSessionLimiter;
        ConcurrencyLimiterOptions _options;

        static Action<ManagedLifetimeLimiter, ILocalSession>  RegistrarDelegate = Registrar;
        static RateLimitPartition<ILocalSession> NoLimiter=RateLimitPartition.GetNoLimiter<ILocalSession>(null!);


        public ActiveSessionsPerGroupLimiter(ConcurrencyLimiterOptions options)
        {
            _options = options;
            Func<HttpContext, RateLimitPartition<ILocalSession>> partitioner = Partitioner;
            _newActiveSessionLimiter = PartitionedRateLimiter.Create(
                partitioner, 
                Comparer);
        }

        public override RateLimiterStatistics? GetStatistics(HttpContext context)
        {
            return context.GetLocalSession().IsAvailable ? _newActiveSessionLimiter.GetStatistics(context) : null;
        }

        protected override async ValueTask<RateLimitLease> AcquireAsyncCore(HttpContext context, Int32 permitCount, CancellationToken token)
        {
            if(HasAssociatedLease(context)) return SuccessLease;
            RateLimitLease lease = await _newActiveSessionLimiter.AcquireAsync(context, permitCount, token);
            return AssociateAndSubstituteLeaseWithSuccess(lease, context);
        }

        protected override RateLimitLease AttemptAcquireCore(HttpContext context, Int32 permitCount)
        {
            if(HasAssociatedLease(context)) return SuccessLease;
            RateLimitLease lease = _newActiveSessionLimiter.AttemptAcquire(context, permitCount);
            return AssociateAndSubstituteLeaseWithSuccess(lease, context);
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

        Boolean HasAssociatedLease(HttpContext context)
        {
            if(!context.GetLocalSession().IsAvailable) return true;
            IActiveSession active_session = context.GetActiveSession();
            return !active_session.IsAvailable || active_session.Properties.ContainsKey(ActiveSessionLeaseInfo.KEY);
        }

        RateLimitLease AssociateAndSubstituteLeaseWithSuccess(RateLimitLease lease, HttpContext context)
        {
            IActiveSession active_session = context.GetActiveSession();
            if(!lease.IsAcquired) return lease;
            try {
                active_session.Properties.Add(ActiveSessionLeaseInfo.KEY, new ActiveSessionLeaseInfo(lease));
                active_session.TakeOwnership(lease);
            }
            catch(ArgumentException) {
                lease.Dispose();
                throw new InvalidOperationException($"An active session lease is already associated with the active session with Id={active_session.Id}");
            }
            catch {
                lease.Dispose();
                throw;
            }
            return SuccessLease;
        }

        RateLimitPartition<ILocalSession> Partitioner(HttpContext context)
        {   
            ILocalSession local_session = context.GetLocalSession();
            // The section limiter must not become expired until the session group object is not disposed.
            // The session group of the context is always available due to HasAssociatedLease call in both lease acquisition methods,
            // so use ManagedLifetimeLimiter associated with the actual session group.
            return ManagedLifetimePartition.GetManagedLifetimeLimiter(
                context.GetLocalSession(),
                key=>RateLimitPartition.GetConcurrencyLimiter(key, key=>_options),
                RegistrarDelegate);
        }

        static void Registrar(ManagedLifetimeLimiter limiter, ILocalSession sessionGroup)
        {
            try {
                sessionGroup.Properties.Add(SessionGroupASLimiterInfo.KEY, new SessionGroupASLimiterInfo(limiter));
                sessionGroup.TakeOwnership(limiter);
            }
            catch(ArgumentException) {
                limiter.Dispose();
                throw new InvalidOperationException($"An active session limiter is already associated with the group with Id={sessionGroup.Id}");
            }
            catch {
                limiter.Dispose();
                throw;
            }
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
