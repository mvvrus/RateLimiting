using System.Threading.RateLimiting;

namespace MVVRus.Extensions.RateLimiting
{
    public static  class ManagedLifetimePartition
    {
        public static RateLimitPartition<TKey> GetManagedLifetimeLimiter<TKey>(
            TKey key,
            Func<TKey, RateLimiter> limiterFactory,
            Action<ManagedLifetimeLimiter, TKey, Object?> registrar,
            Object? state = null)
        {
            return new RateLimitPartition<TKey>(key, Key => {
                ManagedLifetimeLimiter limiter = new ManagedLifetimeLimiter(limiterFactory(Key));
                registrar.Invoke(limiter, Key, state);
                return limiter;
            });
        }

        public static RateLimitPartition<TKey> GetManagedLifetimeLimiter<TKey>(
            TKey key,
            Func<TKey, RateLimitPartition<TKey>> partitionFactory,
            Action<ManagedLifetimeLimiter, TKey, Object?> registrar,
            Object? state = null)
        {
            return new RateLimitPartition<TKey>(key, Key => {
                ManagedLifetimeLimiter limiter = new ManagedLifetimeLimiter(partitionFactory(Key).Factory(Key));
                registrar.Invoke(limiter, Key, state);
                return limiter;
            });
        }

    }
}
