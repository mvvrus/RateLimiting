
using Microsoft.Extensions.Primitives;

namespace MVVRus.AspNetCore.RateLimiting
{
    public sealed class BackLinkImpl : IHttpContextBackLinkFeature, IEquatable<BackLinkImpl>
    {
        const int HASH_MAGIC = 0x3c3c3c3c;

        static Int64 s_key = 0;
        HttpContext? _backLink;
        Int64 _key;
        CancellationTokenSource _tokenSource;
        CancellationChangeToken _token;

        public HttpContext BackLink => Volatile.Read(ref  _backLink)??throw new InvalidOperationException("No HttpContext backlink exists.");
        
        public IChangeToken ChangeToken => _token;

        internal BackLinkImpl(HttpContext context)
        {
            _backLink = context;
            _key=Interlocked.Increment(ref s_key);
            _tokenSource = new CancellationTokenSource();
            _token = new CancellationChangeToken(_tokenSource.Token);
        }

        public override Int32 GetHashCode()
        {
            return _key.GetHashCode()^HASH_MAGIC;
        }

        public override Boolean Equals(Object? obj)
        {
            if(obj is BackLinkImpl other) return Equals(other);
            else return false;
        }

        public Boolean Equals(BackLinkImpl? other)
        {
            return other is null?false:this._key== other._key;
        }

        Boolean IEquatable<IHttpContextBackLinkFeature>.Equals(IHttpContextBackLinkFeature? intf)
        {
            if(intf is BackLinkImpl other) return Equals(other);
            else return false;
        }

        public void Dispose()
        {
            _tokenSource.Cancel();
            _tokenSource.Dispose();
            Interlocked.Exchange(ref _backLink, null);
        }
    }
}
