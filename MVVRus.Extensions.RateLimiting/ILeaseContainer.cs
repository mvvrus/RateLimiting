using System.Threading.RateLimiting;

namespace MVVRus.Extensions.RateLimiting
{
    public interface ILeaseContainer
    {
        Boolean TryGetLease(out RateLimitLease? lease);
        Boolean TrySetLease(ref RateLimitLease lease);
    }
}
