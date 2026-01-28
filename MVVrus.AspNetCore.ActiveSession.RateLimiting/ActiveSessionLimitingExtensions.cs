using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace MVVrus.AspNetCore.ActiveSession.RateLimiting
{
    public static class ActiveSessionLimitingExtensions
    {
        public static IApplicationBuilder UseActiveSessionPerGroupLimiting(this IApplicationBuilder app)
        {
            app.UseMiddleware<ActiveSessionLimitingMiddleware>();
            return app;
        }

        public static IServiceCollection AddActiveSessionPerGroupLimiting(this IServiceCollection services, ConcurrencyLimiterOptions limiterOptions)
        {
            services.PostConfigure<RateLimiterOptions>(AddActiveSessionPerGroupLimiter);
            return services;

            void AddActiveSessionPerGroupLimiter(RateLimiterOptions options)
            {
                ActiveSessionsPerGroupLimiter limiter = new ActiveSessionsPerGroupLimiter(limiterOptions);
                options.GlobalLimiter = options.GlobalLimiter==null ? 
                    limiter : PartitionedRateLimiter.CreateChained(options.GlobalLimiter, limiter);
            }

        }

    }
}
