using Microsoft.Extensions.Primitives;
using System.Threading.RateLimiting;

namespace MVVRus.AspNetCore.RateLimiting
{
    public class SelectiveRateLimiterAdapter: RateLimiter
    {
        PartitionedRateLimiter<HttpContext> _limiter;
        IHttpContextBackLinkFeature _key;
        IDisposable? _backlinkDisposeReg;
        static Action<Object?> _backlinkDisposeCallback= BacklinkDisposeCallback;

        public SelectiveRateLimiterAdapter(PartitionedRateLimiter<HttpContext> limiter, IHttpContextBackLinkFeature key)
        {
            _limiter = limiter;
            _key = key;
            IChangeToken token = _key.ChangeToken;
            if(!token.ActiveChangeCallbacks) 
                throw new InvalidOperationException("SelectiveRateLimiterAdapter: change token doesn't support a callback.");
            _backlinkDisposeReg = token.RegisterChangeCallback(_backlinkDisposeCallback, this);
        }

        public override TimeSpan? IdleDuration => Volatile.Read(ref _backlinkDisposeReg) is null? TimeSpan.MaxValue : null;

        public override RateLimiterStatistics? GetStatistics()
        {
            return _limiter.GetStatistics(_key.BackLink);
        }

        protected override ValueTask<RateLimitLease> AcquireAsyncCore(Int32 permitCount, CancellationToken cancellationToken)
        {
            return _limiter.AcquireAsync(_key.BackLink, permitCount, cancellationToken);
        }

        protected override RateLimitLease AttemptAcquireCore(Int32 permitCount)
        {
            return _limiter.AttemptAcquire(_key.BackLink, permitCount);
        }

        protected override void Dispose(Boolean disposing)
        {
            if(disposing) ReleaseBackLinkDisposeReg()?.Dispose();
            base.Dispose(disposing);
        }

        protected override ValueTask DisposeAsyncCore()
        {
            ReleaseBackLinkDisposeReg()?.Dispose();
            return base.DisposeAsyncCore(); 
        }

        IDisposable? ReleaseBackLinkDisposeReg()
        {
            return Interlocked.Exchange(ref _backlinkDisposeReg, null);
        }

        static void BacklinkDisposeCallback(Object? this_ref) 
        {
            ((SelectiveRateLimiterAdapter?)this_ref)?.ReleaseBackLinkDisposeReg();
        }
    }
}
