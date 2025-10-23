using Microsoft.Extensions.Primitives;
using System.Threading.RateLimiting;

namespace MVVRus.AspNetCore.RateLimiting
{
    public class SelectiveRateLimiterAdapter: RateLimiter
    {
        PartitionedRateLimiter<HttpContext> _limiter;
        IHttpContextBackLinkFeature? _key;

        public SelectiveRateLimiterAdapter(PartitionedRateLimiter<HttpContext> limiter, IHttpContextBackLinkFeature key)
        {
            _limiter = limiter;
            _key = key;
            _key.DisposedEvent+= BacklinkDisposeCallback;
        }

        public override TimeSpan? IdleDuration => Volatile.Read(ref _key) is null? TimeSpan.MaxValue : null;

        public override RateLimiterStatistics? GetStatistics()
        {
            IHttpContextBackLinkFeature? key = Volatile.Read(ref _key);
            if(key == null) throw new ObjectDisposedException(nameof(GetStatistics));
            return _limiter.GetStatistics(key.BackLink);
        }

        protected override ValueTask<RateLimitLease> AcquireAsyncCore(Int32 permitCount, CancellationToken cancellationToken)
        {
            IHttpContextBackLinkFeature? key = Volatile.Read(ref _key);
            if(key == null) throw new ObjectDisposedException(nameof(AcquireAsyncCore));
            return _limiter.AcquireAsync(key.BackLink, permitCount, cancellationToken);
        }

        protected override RateLimitLease AttemptAcquireCore(Int32 permitCount)
        {
            IHttpContextBackLinkFeature? key = Volatile.Read(ref _key);
            if(key == null) throw new ObjectDisposedException(nameof(AttemptAcquireCore));
            return _limiter.AttemptAcquire(key.BackLink, permitCount);
        }

        protected override void Dispose(Boolean disposing)
        {
            if(disposing) UnregisterDisposeCallback();
            base.Dispose(disposing);
        }

        protected override ValueTask DisposeAsyncCore()
        {
            UnregisterDisposeCallback();
            //ReleaseBackLinkDisposeReg()?.Dispose();
            return base.DisposeAsyncCore(); 
        }

        void UnregisterDisposeCallback()
        {
            IHttpContextBackLinkFeature? key = Interlocked.Exchange(ref _key, null);
            if(key!=null) key.DisposedEvent-=BacklinkDisposeCallback;
        }

        void BacklinkDisposeCallback(Object? sender, EventArgs e) 
        {
            UnregisterDisposeCallback();
        }
    }
}
