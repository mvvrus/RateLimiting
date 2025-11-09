namespace MVVrus.AspNetCore.ActiveSession.RateLimiting
{
    public static class ActiveSessionLimitingExtensions
    {
        public static IApplicationBuilder UseActiveSessionLimiting(this IApplicationBuilder app)
        {
            app.UseMiddleware<ActiveSessionLimitingMiddleware>();
            return app;
        }
    }
}
