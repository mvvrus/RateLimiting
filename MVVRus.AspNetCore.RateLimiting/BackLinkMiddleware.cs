namespace MVVRus.AspNetCore.RateLimiting
{
    internal class BackLinkMiddleware
    {
        private readonly RequestDelegate _next;

        public BackLinkMiddleware(RequestDelegate next) 
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            IHttpContextBackLinkFeature back_link = new BackLinkImpl(context);
            try {
                context.Features[typeof(IHttpContextBackLinkFeature)] = back_link;
                await _next(context);
            }
            finally {
                context.Features[typeof(IHttpContextBackLinkFeature)] = null;
                back_link.Dispose();
            }
        }
    }
}
