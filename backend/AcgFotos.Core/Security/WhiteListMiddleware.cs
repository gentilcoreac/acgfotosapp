using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace AcgFotos.Core.Security.Middleware
{
    public class WhiteListMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly string _adminWhiteList;

        public WhiteListMiddleware(RequestDelegate next, string adminWhiteList)
        {
            _next = next;
            _adminWhiteList = adminWhiteList;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            // ForwardedHeaders middleware (configurado en Startup) ya re-escribe RemoteIpAddress
            // con el valor de X-Forwarded-For cuando hay proxy delante, así que esto da
            // la IP real del cliente.
            var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();

            var ips = _adminWhiteList.Split(';');

            if (!ips.Any(option => option == ipAddress))
            {
                httpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                httpContext.Response.Headers.Append("x-ip-check", "IP not allowed");
                return;
            }

            await _next(httpContext);
        }
    }

    public static class WhiteListMiddlewareExtensions
    {
        public static IApplicationBuilder UseWhiteListMiddleware(this IApplicationBuilder builder,
                                                                       string whiteList)
        {
            return builder.UseMiddleware<WhiteListMiddleware>(whiteList);
        }
    }
}
