using System.Threading.RateLimiting;

namespace MVVRus.Extensions.RateLimiting
{
    public interface IShareableLeaseOwner:IDisposable
    {
        void Release(DerivedLease derivedLease);
        event EventHandler DisposedEvent;
    }

    public interface IShareableLeaseOwner<TResource>: IShareableLeaseOwner
    {
        DerivedLease AcquireLease(TResource resource, Int32 permitCount);
        ValueTask<DerivedLease> AcquireLeaseAsync(TResource resource, Int32 permitCount, CancellationToken cancellationToken);
    }
}
