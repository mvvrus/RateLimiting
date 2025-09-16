using Microsoft.Extensions.Primitives;

namespace MVVRus.AspNetCore.RateLimiting
{
    public interface IHttpContextBackLinkFeature: IDisposable, IEquatable<IHttpContextBackLinkFeature>
    {
        HttpContext BackLink { get; }
        IChangeToken ChangeToken { get; }
    }
}
