using MVVRus.Extensions.RateLimiting;
using System.Diagnostics.CodeAnalysis;
using System.Threading.RateLimiting;

namespace MVVrus.AspNetCore.ActiveSession.RateLimiting
{
    public class ActiveSessionsPerGroupLimiter : LinkedLeaseRateLimiter<HttpContext, ILocalSession>
    {

        static Action<ManagedLifetimeLimiter, ILocalSession> RegistrarDelegate = Registrar;

        public ActiveSessionsPerGroupLimiter(ConcurrencyLimiterOptions options) 
            : base(PartitionedRateLimiter.Create(PartitionerMaker(options), Comparer), true) { }

        protected override IShareableLeaseOwner<HttpContext>? GetLeaseStore(HttpContext context)
        {

            IActiveSession? active_session = context.GetActiveSession();
            ActiveSessionLeaseInfo? lease_info = null;

            if(active_session == null || !active_session.IsAvailable) return null;

            try {
                active_session.Properties.Add(ActiveSessionLeaseInfo.KEY, lease_info=new ActiveSessionLeaseInfo());
                active_session.TakeOwnership(lease_info);
            }
            catch(ArgumentException) {
                lease_info?.Dispose();
                throw new InvalidOperationException($"An active session lease is already associated with the active session with Id={active_session.Id}");
            }
            catch {
                lease_info?.Dispose();
                throw;
            }
            return lease_info;
        }

        static Func<HttpContext, RateLimitPartition<ILocalSession>> PartitionerMaker(ConcurrencyLimiterOptions options)
        {
            // The section limiter must not become expired until the session group object is not disposed.
            // The session group of the context is always available due to HasAssociatedLease call in both lease acquisition methods,
            // so use ManagedLifetimeLimiter associated with the actual session group.

            return Partitioner;

            RateLimitPartition<ILocalSession> Partitioner(HttpContext context)
            {
                ILocalSession group = context.GetActiveSessionGroup();
                Func<ILocalSession, RateLimiter> factory =
                    key => (key.IsAvailable ?
                            ManagedLifetimePartition.GetManagedLifetimeLimiter(
                                key,
                                gclKey => RateLimitPartition.GetConcurrencyLimiter(gclKey, gclKey => options),
                                RegistrarDelegate
                            )
                        : RateLimitPartition.GetNoLimiter(key)
                    ).Factory(key);
                return new RateLimitPartition<ILocalSession>(group.IsAvailable ? group : NullGroup, factory);                    
            }

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

        class DummmySessionGroup : ILocalSession
        {
            public String Id => "{416DFAB3-DF7A-4FB8-B294-73A9D813B869}";

            public Boolean IsAvailable => false;

            public IServiceProvider SessionServices => throw new NotImplementedException();

            public CancellationToken CompletionToken => throw new NotImplementedException();

            public IDictionary<String, Object> Properties => throw new NotImplementedException();
        }

        static DummmySessionGroup NullGroup = new DummmySessionGroup();

    }
}
