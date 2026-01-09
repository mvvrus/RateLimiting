using System.Threading.RateLimiting;

namespace MVVRus.Extensions.RateLimiting
{
    public static  class ManagedLifetimePartition
    {
        public static RateLimitPartition<TKey> GetManagedLifetimeLimiter<TKey>(
            TKey key,
            Func<TKey, RateLimiter> factory,
            Action<RateLimiter, TKey> registrar)
        {
            return new RateLimitPartition<TKey>(key, Key => {
                RateLimiter limiter = MakeAggregateLimiter(Key,factory);
                registrar.Invoke(limiter, Key);
                return limiter;
            });
        }

        public static RateLimitPartition<TKey> GetManagedLifetimeLimiter<TKey>(
            TKey key,
            Func<TKey, RateLimitPartition<TKey>> factory,
            Action<RateLimiter, TKey> registrar)
        {
            return new RateLimitPartition<TKey>(key, Key => {
                RateLimiter limiter = MakeAggregateLimiter(Key, factory(Key).Factory);
                registrar.Invoke(limiter, Key);
                return limiter;
            });
        }

        public static RateLimitPartition<TKey> GetManagedLifetimeLimiter<TKey, TContext>(
            TKey key,
            Func<TKey, RateLimiter> factory,
            Action<RateLimiter, TKey, TContext> registrar,
            TContext context)
        {
            return new RateLimitPartition<TKey>(key, Key => {
                RateLimiter limiter = MakeAggregateLimiter(Key, factory);
                registrar.Invoke(limiter, Key, context);
                return limiter;
            });
        }

        public static RateLimitPartition<TKey> GetManagedLifetimeLimiter<TKey, TContext>(
            TKey key,
            Func<TKey, RateLimitPartition<TKey>> factory,
            Action<RateLimiter, TKey, TContext> registrar,
            TContext context)
        {
            return new RateLimitPartition<TKey>(key, Key => {
                RateLimiter limiter = MakeAggregateLimiter(Key, factory(Key).Factory);
                registrar.Invoke(limiter, Key, context);
                return limiter;
            });
        }

        static RateLimiter MakeAggregateLimiter<TKey>(TKey Key, Func<TKey, RateLimiter> factory)
        {
            RateLimiter inner=factory.Invoke(Key);
            if(inner is ReplenishingRateLimiter replenishing_inner) return new ManagedLifetimeReplenishingLimiter(replenishing_inner);
            else return new ManagedLifetimeLimiter(inner);
        }

    }
}
