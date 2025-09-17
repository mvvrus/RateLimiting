namespace MVVRus.AspNetCore.RateLimiting
{
    public static class BackLinkBuilderExtensions
    {
        public static IApplicationBuilder UseBackLink(this IApplicationBuilder app) 
        {
            return app.UseMiddleware<BackLinkMiddleware>();
        }
    }
}
