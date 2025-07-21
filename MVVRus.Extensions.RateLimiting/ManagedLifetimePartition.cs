using System.Threading.RateLimiting;

namespace MVVRus.Extensions.RateLimiting
{
    public static  class ManagedLifetimePartition
    {
        public static Func<TKey, RateLimitPartition<TKey>> GetManagedLifetimeLimiter<TKey>(
            Func<TKey, RateLimiter> inner,
            Action<ManagedLifetimeLimiter, TKey, Object?>? registrar = null,
            Object? state = null)
        {
            return (TKey key) => {
                ManagedLifetimeLimiter limiter = new ManagedLifetimeLimiter(inner(key));
                registrar?.Invoke(limiter, key, state);
                return new RateLimitPartition<TKey>(key, _ => limiter);
            };
        }

        public static Func<TKey, RateLimitPartition<TKey>> GetManagedLifetimeLimiter<TKey>(
            Func<TKey, RateLimitPartition<TKey>> inner,
            Action<ManagedLifetimeLimiter, TKey, Object?>? registrar = null,
            Object? state = null)
        {
            return GetManagedLifetimeLimiter(key=>inner(key).Factory(key), registrar, state);
        }

    }
}
