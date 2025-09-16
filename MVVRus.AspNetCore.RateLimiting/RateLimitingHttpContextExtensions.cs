namespace MVVRus.AspNetCore.RateLimiting
{
    public static class RateLimitingHttpContextExtensions
    {
        public static IHttpContextBackLinkFeature? GetBackLinkFeature(this HttpContext context) {
            ArgumentNullException.ThrowIfNull(context);
            return context.Features[typeof(IHttpContextBackLinkFeature)] as IHttpContextBackLinkFeature;
        }
    }
}
