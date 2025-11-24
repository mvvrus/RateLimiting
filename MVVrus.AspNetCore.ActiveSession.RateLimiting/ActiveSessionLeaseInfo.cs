using MVVRus.Extensions.RateLimiting;
using System.Threading.RateLimiting;

namespace MVVrus.AspNetCore.ActiveSession.RateLimiting
{
    internal class ActiveSessionLeaseInfo : SingleShareableLeaseOwner<HttpContext>
    {
        public const String KEY = "{9A54776E-156B-470D-9431-C293E179B9EB}";

        RateLimitLease? _lease;
        public Boolean WasLeaseRejected { get; private set; } = false;

        protected override void Dispose(Boolean disposing)
        {
            if(disposing) {
                RateLimitLease? lease = Interlocked.Exchange(ref _lease, null);
                lease?.Dispose();
            }
            base.Dispose(disposing);
        }

        protected override Boolean TryGetLease(out RateLimitLease? lease)
        {
            lease = Volatile.Read(ref _lease);
            return lease != null;
        }

        protected override Boolean TrySetLease(ref RateLimitLease lease)
        {
            if(!lease.IsAcquired) {
                WasLeaseRejected = true;
                return false;
            }
            return Interlocked.CompareExchange(ref _lease, lease, null)!=null;
        }
    }
}
