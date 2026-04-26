using Microsoft.AspNetCore.Http;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Prosim2GSX.Web.Middleware
{
    // Bearer-token gate for every REST request. Reads Config.WebServerAuthToken
    // on every call (no caching) so a regenerate-token operation invalidates
    // existing tokens immediately.
    //
    // The /ws path is exempt — WebSocket auth uses the first-frame
    // { "auth": "<token>" } pattern (Phase 6C) because browsers cannot set
    // custom headers on a WebSocket upgrade and putting the token in the URL
    // would leak it to access logs and browser history.
    public class BearerTokenMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly AppService _app;

        public BearerTokenMiddleware(RequestDelegate next, AppService app)
        {
            _next = next;
            _app = app;
        }

        public async Task Invoke(HttpContext context)
        {
            // WS upgrade path is auth'd by its own handler.
            if (context.Request.Path.StartsWithSegments("/ws"))
            {
                await _next(context);
                return;
            }

            // CORS preflight must succeed without auth so the browser can ask.
            if (HttpMethods.IsOptions(context.Request.Method))
            {
                await _next(context);
                return;
            }

            var expected = _app?.Config?.WebServerAuthToken;
            if (string.IsNullOrEmpty(expected))
            {
                // Server enabled but token not configured — reject with 503 so
                // the client knows it's a server problem, not a credential
                // problem. The host should never let this state happen because
                // it generates a token on first start, but defensive.
                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                return;
            }

            string presented = null;
            string authHeader = context.Request.Headers.Authorization.ToString();
            if (!string.IsNullOrEmpty(authHeader)
                && authHeader.StartsWith("Bearer ", StringComparison.Ordinal))
            {
                presented = authHeader.Substring(7).Trim();
            }

            if (string.IsNullOrEmpty(presented) || !FixedTimeEquals(presented, expected))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            await _next(context);
        }

        // Constant-time comparison so token-validation timing can't be used to
        // recover the token byte-by-byte. Closed home LAN, but defense-in-depth
        // is cheap and CryptographicOperations.FixedTimeEquals is in-box.
        private static bool FixedTimeEquals(string a, string b)
        {
            if (a == null || b == null) return false;
            var aBytes = Encoding.UTF8.GetBytes(a);
            var bBytes = Encoding.UTF8.GetBytes(b);
            return aBytes.Length == bBytes.Length
                && CryptographicOperations.FixedTimeEquals(aBytes, bBytes);
        }
    }
}
