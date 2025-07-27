using System.Threading.RateLimiting;

namespace MVVrus.AspNetCore.ActiveSession.RateLimiting
{
    internal class ActiveSessionLeaseInfo : IDisposable
    {
        public const String KEY = "{9A54776E-156B-470D-9431-C293E179B9EB}";

        RateLimitLease? _lease;

        public RateLimitLease Lease => _lease?? throw new ObjectDisposedException(nameof(ActiveSessionLeaseInfo));

        public ActiveSessionLeaseInfo(RateLimitLease lease)
        {
            _lease = lease;
        }

        public void Dispose()
        {
            RateLimitLease? lease = Interlocked.Exchange(ref _lease, null);
            if(lease != null) lease.Dispose();
        }
    }
}
