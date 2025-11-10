using MVVRus.Extensions.RateLimiting;
using System.Diagnostics.CodeAnalysis;
using System.Threading.RateLimiting;

namespace MVVrus.AspNetCore.ActiveSession.RateLimiting
{
    public class ActiveSessionsPerGroupLimiter : LinkedLeaseRateLimiter<HttpContext, ILocalSession>
    {

        static Action<ManagedLifetimeLimiter, ILocalSession> RegistrarDelegate = Registrar;

        public ActiveSessionsPerGroupLimiter(ConcurrencyLimiterOptions options) 
            : base(SessionGroupExtractor,
                  PartitionerMaker(options),
                  Comparer)
        {

        }

        protected override IShareableLeaseOwner<HttpContext>? GetLeaseStore(HttpContext context)
        {

            IActiveSession? active_session = context.GetActiveSession();
            ActiveSessionLeaseInfo? lease_info = null;

            if(active_session == null || !active_session.IsAvailable) return null;

            try {
                active_session.Properties.Add(ActiveSessionLeaseInfo.KEY, lease_info=new ActiveSessionLeaseInfo(this.BaseLimiter));
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

        static Func<ILocalSession, RateLimitPartition<ILocalSession>> PartitionerMaker(ConcurrencyLimiterOptions options)
        {
            // The section limiter must not become expired until the session group object is not disposed.
            // The session group of the context is always available due to HasAssociatedLease call in both lease acquisition methods,
            // so use ManagedLifetimeLimiter associated with the actual session group.
            return (ILocalSession local_session) => local_session.IsAvailable? 
                    ManagedLifetimePartition.GetManagedLifetimeLimiter(
                        local_session,
                        key => RateLimitPartition.GetConcurrencyLimiter(key, key => options),
                        RegistrarDelegate) 
                : RateLimitPartition.GetNoLimiter(local_session);
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

        static ILocalSession SessionGroupExtractor(HttpContext context) 
        {
            ILocalSession group = context.GetActiveSessionGroup();
            return group.IsAvailable ? group : NullGroup;
        }

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
