using System.Threading.RateLimiting;

namespace MVVRus.Extensions.RateLimiting
{
    public static  class ManagedLifetimePartition
    {
        public static RateLimitPartition<TKey> GetManagedLifetimeLimiter<TKey>(
            TKey key,
            Func<TKey, RateLimiter> factory,
            Action<ManagedLifetimeLimiter, TKey> registrar)
        {
            return new RateLimitPartition<TKey>(key, Key => {
                ManagedLifetimeLimiter limiter = new ManagedLifetimeLimiter(factory(Key));
                registrar.Invoke(limiter, Key);
                return limiter;
            });
        }

        public static RateLimitPartition<TKey> GetManagedLifetimeLimiter<TKey>(
            TKey key,
            Func<TKey, RateLimitPartition<TKey>> factory,
            Action<ManagedLifetimeLimiter, TKey> registrar)
        {
            return new RateLimitPartition<TKey>(key, Key => {
                ManagedLifetimeLimiter limiter = new ManagedLifetimeLimiter(factory(Key).Factory(Key));
                registrar.Invoke(limiter, Key);
                return limiter;
            });
        }

        public static RateLimitPartition<TKey> GetManagedLifetimeLimiter<TKey, TContext>(
            TKey key,
            Func<TKey, RateLimiter> factory,
            Action<ManagedLifetimeLimiter, TKey, TContext> registrar,
            TContext context)
        {
            return new RateLimitPartition<TKey>(key, Key => {
                ManagedLifetimeLimiter limiter = new ManagedLifetimeLimiter(factory(Key));
                registrar.Invoke(limiter, Key, context);
                return limiter;
            });
        }

        public static RateLimitPartition<TKey> GetManagedLifetimeLimiter<TKey, TContext>(
            TKey key,
            Func<TKey, RateLimitPartition<TKey>> factory,
            Action<ManagedLifetimeLimiter, TKey, TContext> registrar,
            TContext context)
        {
            return new RateLimitPartition<TKey>(key, Key => {
                ManagedLifetimeLimiter limiter = new ManagedLifetimeLimiter(factory(Key).Factory(Key));
                registrar.Invoke(limiter, Key, context);
                return limiter;
            });
        }

    }
}
