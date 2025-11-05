
using System.Threading.RateLimiting;

namespace MVVRus.Extensions.RateLimiting
{
    public class DerivedLease : RateLimitLease
    {
        IShareableLeaseOwner? _owner;
        IDictionary<String, Object?> _metadata;

        volatile Boolean _need_release = true;

        public DerivedLease(IShareableLeaseOwner owner, Boolean isAcquired, Int32 permitCount, IDictionary<String,Object?> metadata)
        {
            _owner=owner;
            IsAcquired=isAcquired;
            _metadata=metadata;
            _owner.DisposedEvent+=OwnerDisposeHandler;
        }


        public override Boolean IsAcquired { get; }
        public override IEnumerable<String> MetadataNames=>_metadata.Keys;
        public override Boolean TryGetMetadata(String metadataName, out Object? metadata)
        {
            if(_metadata.ContainsKey(metadataName)) { metadata = _metadata[metadataName]; return true; }
            else { metadata = null;  return false; }
        }

        public Int32 PermitCount { get; }

        void OwnerDisposeHandler(Object? Sender, EventArgs toIgnore)
        {
            _need_release=false;
            Dispose();
        }


        protected override void Dispose(Boolean disposing)
        {
            if(disposing) {
                IShareableLeaseOwner? owner = Interlocked.Exchange(ref _owner, null);
                if(owner != null) owner.DisposedEvent -= OwnerDisposeHandler;
                if(_need_release) owner?.Release(this);
            }
        }
    }
}
