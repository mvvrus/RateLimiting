using MVVRus.Extensions.RateLimiting;
using System.Threading.RateLimiting;

namespace MVVrus.AspNetCore.ActiveSession.RateLimiting
{
    internal class ActiveSessionLeaseInfo : IDisposable, ILeaseContainer
    {
        public const String KEY = "{9A54776E-156B-470D-9431-C293E179B9EB}";

        RateLimitLease? _lease;
        public IShareableLeaseOwner<HttpContext>? LeaseOwner { get; }

        public RateLimitLease Lease => _lease?? throw new ObjectDisposedException(nameof(ActiveSessionLeaseInfo));

        public ActiveSessionLeaseInfo(RateLimitLease lease)
        {
            _lease = lease;
        }

        public ActiveSessionLeaseInfo(IRawRateLimiter<HttpContext> baseLimiter)
        {
            LeaseOwner = new SingleShareableLeaseOwner<HttpContext>(baseLimiter, this);
        }

        public void Dispose()
        {
            LeaseOwner?.Dispose();
            RateLimitLease? lease = Interlocked.Exchange(ref _lease, null);
            lease?.Dispose();
        }

        public Boolean TryGetLease(out RateLimitLease? lease)
        {
            lease = Volatile.Read(ref _lease);
            return lease != null;
        }

        public Boolean TrySetLease(ref RateLimitLease lease)
        {
            if(!lease.IsAcquired) return false;
            return Interlocked.CompareExchange(ref _lease, lease, null)!=null;
        }
    }
}
