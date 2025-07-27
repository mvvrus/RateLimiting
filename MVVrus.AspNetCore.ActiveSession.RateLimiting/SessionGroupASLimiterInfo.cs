using MVVRus.Extensions.RateLimiting;

namespace MVVrus.AspNetCore.ActiveSession.RateLimiting
{
    internal class SessionGroupASLimiterInfo : IDisposable
    {
        public const String KEY = "{E7A63EB5-7CF1-433A-AA2B-9483FF3D34BE}";

        ManagedLifetimeLimiter? _sectionLimiter;

        ManagedLifetimeLimiter SectionLimiter => _sectionLimiter ?? throw new ObjectDisposedException(nameof(SectionLimiter));

        public SessionGroupASLimiterInfo(ManagedLifetimeLimiter sectionLimiter)
        {
            _sectionLimiter = sectionLimiter;
        }

        public void Dispose()
        {
            ManagedLifetimeLimiter? sectionLimiter = Interlocked.Exchange(ref _sectionLimiter, null);
            if(sectionLimiter != null) sectionLimiter.Dispose();
        }
    }
}
