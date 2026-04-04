namespace Shop.API.Middleware;

public class SsoCookieMiddleware
{
    private readonly RequestDelegate _next;
    private const string SSO_COOKIE = "syndock_sso";
    private const string SSO_USER_COOKIE = "syndock_user";

    public SsoCookieMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        // If no Authorization header but SSO cookie exists, use cookie token
        if (!context.Request.Headers.ContainsKey("Authorization"))
        {
            var ssoToken = context.Request.Cookies[SSO_COOKIE];
            if (!string.IsNullOrEmpty(ssoToken))
            {
                context.Request.Headers.Append("Authorization", $"Bearer {ssoToken}");
            }
        }

        // Only intercept response body for login endpoints (to set SSO cookie)
        var path = context.Request.Path.Value?.ToLower() ?? "";
        if (!path.Contains("/auth/login") && !path.Contains("/mall/auth/login"))
        {
            await _next(context);
            return;
        }

        // Capture the response to set cookie on login
        var originalBody = context.Response.Body;
        using var memStream = new MemoryStream();
        context.Response.Body = memStream;

        await _next(context);

        memStream.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(memStream).ReadToEndAsync();
        memStream.Seek(0, SeekOrigin.Begin);

        // Check if this is a login response with accessToken
        if (context.Response.StatusCode == 200 &&
            (path.Contains("/auth/login") || path.Contains("/mall/auth/login")) &&
            responseBody.Contains("accessToken"))
        {
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(responseBody);
                string? token = null;
                string? userName = null;

                // Navigate different response structures
                if (doc.RootElement.TryGetProperty("data", out var data))
                {
                    if (data.TryGetProperty("auth", out var auth) && auth.TryGetProperty("accessToken", out var t))
                    {
                        token = t.GetString();
                        if (auth.TryGetProperty("user", out var u) && u.TryGetProperty("name", out var n))
                            userName = n.GetString();
                    }
                }
                else if (doc.RootElement.TryGetProperty("auth", out var authDirect))
                {
                    if (authDirect.TryGetProperty("accessToken", out var t))
                    {
                        token = t.GetString();
                        if (authDirect.TryGetProperty("user", out var u) && u.TryGetProperty("name", out var n))
                            userName = n.GetString();
                    }
                }

                if (!string.IsNullOrEmpty(token))
                {
                    var cookieOptions = new CookieOptions
                    {
                        HttpOnly = false, // Frontend JS needs to read it for other domains
                        Secure = context.Request.IsHttps,
                        SameSite = SameSiteMode.Lax,
                        Path = "/",
                        Domain = GetCookieDomain(context.Request.Host.Host),
                        MaxAge = TimeSpan.FromDays(7)
                    };

                    context.Response.Cookies.Append(SSO_COOKIE, token, cookieOptions);
                    if (userName != null)
                        context.Response.Cookies.Append(SSO_USER_COOKIE, userName, cookieOptions);
                }
            }
            catch { /* Parsing failure shouldn't break login */ }
        }

        // Check if this is a logout - clear cookies
        if (path.Contains("/auth/logout"))
        {
            context.Response.Cookies.Delete(SSO_COOKIE);
            context.Response.Cookies.Delete(SSO_USER_COOKIE);
        }

        await memStream.CopyToAsync(originalBody);
        context.Response.Body = originalBody;
    }

    private static string? GetCookieDomain(string host)
    {
        // In production: set to .syndock.com for all subdomains
        if (host.EndsWith("syndock.com"))
            return ".syndock.com";
        // In development: null (current domain only, but same-origin for localhost)
        return null;
    }
}
