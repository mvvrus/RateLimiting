using System.Diagnostics.CodeAnalysis;
using System.Threading.RateLimiting;
using MVVRus.Extensions.RateLimiting;

namespace MVVrus.AspNetCore.ActiveSession.RateLimiting
{
    public class ActiveSessionsPerGroupLimiter : LimiterLeaseTranslator<HttpContext>
    {

        static Action<ManagedLifetimeLimiter, ILocalSession>  RegistrarDelegate = Registrar;
        static RateLimitPartition<ILocalSession> NoLimiter=RateLimitPartition.GetNoLimiter<ILocalSession>(null!);


        public ActiveSessionsPerGroupLimiter(ConcurrencyLimiterOptions options): base (
            PartitionedRateLimiter.Create(
                (HttpContext context) => Partitioner(context, options),
                Comparer)
            ) {}

        public override RateLimiterStatistics? GetStatistics(HttpContext context)
        {
            return context.GetLocalSession().IsAvailable ? base.GetStatistics(context) : null;
        }

        static RateLimitPartition<ILocalSession> Partitioner(HttpContext context, ConcurrencyLimiterOptions options)
        {   
            ILocalSession local_session = context.GetLocalSession();
            // The section limiter must not become expired until the session group object is not disposed.
            // The session group of the context is always available due to HasAssociatedLease call in both lease acquisition methods,
            // so use ManagedLifetimeLimiter associated with the actual session group.
            return ManagedLifetimePartition.GetManagedLifetimeLimiter(
                context.GetLocalSession(),
                key=>RateLimitPartition.GetConcurrencyLimiter(key, key=>options),
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

        protected override RateLimitLease TranslateLease(RateLimitLease lease, HttpContext context)
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

        protected override RateLimitLease? ExtractExistingLease(HttpContext context) {
            if(!context.GetLocalSession().IsAvailable) return SuccessLease;
            IActiveSession active_session = context.GetActiveSession();
            return !active_session.IsAvailable || active_session.Properties.ContainsKey(ActiveSessionLeaseInfo.KEY) 
                ? SuccessLease : null;
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
