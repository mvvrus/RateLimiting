
using System.Threading.RateLimiting;

namespace MVVRus.Extensions.RateLimiting
{
    public class DerivedLease : RateLimitLease
    {
        IShareableLeaseOwner? _owner;
        IDictionary<String, Object?> _metadata;




        public DerivedLease(IShareableLeaseOwner owner, Boolean isAcquired, Int32 permitCount, IDictionary<String,Object?> metadata)
        {
            _owner=owner;
            IsAcquired=isAcquired;
            _metadata=metadata;
        }


        public override Boolean IsAcquired { get; }
        public override IEnumerable<String> MetadataNames=>_metadata.Keys;
        public override Boolean TryGetMetadata(String metadataName, out Object? metadata)
        {
            if(_metadata.ContainsKey(metadataName)) { metadata = _metadata[metadataName]; return true; }
            else { metadata = null;  return false; }
        }

        public Int32 PermitCount { get; }

        protected override void Dispose(Boolean disposing)
        {
            IShareableLeaseOwner? owner=Interlocked.Exchange(ref _owner, null);
            owner?.Release(this);
        }
    }
}
