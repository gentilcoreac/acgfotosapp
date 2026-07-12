using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using AcgFotos.Base.Application.IServices;
using AcgFotos.Core.Security;
using AcgFotos.Core.Session;

namespace AcgFotos.Base.Application.Services
{
    /// <summary>Ver <see cref="IImpersonationManager"/>. ADR-0002.</summary>
    public class ImpersonationManager : IImpersonationManager
    {
        private const string ImpersonationCookieName = "impersonation";
        private const int DefaultRefreshDays = 14;
        private const string CookiePath = "/api/auth";

        private readonly IAppContext _appContext;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;
        private readonly JwtSecurityTokenConfig _jwtConfig;

        public ImpersonationManager(
            IAppContext appContext,
            IHttpContextAccessor httpContextAccessor,
            IConfiguration configuration,
            IOptions<JwtSecurityTokenConfig> jwtOptions)
        {
            _appContext = appContext;
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
            _jwtConfig = jwtOptions.Value;
        }

        public long? ResolveRealRootUserId()
        {
            if (_appContext.ImpersonatedBy.HasValue)
            {
                return _appContext.ImpersonatedBy.Value;
            }
            if (_appContext.IsRoot)
            {
                return _appContext.UserId;
            }
            return null;
        }

        public TokenModelDto ToDto(TokenModel tokenModel) => new TokenModelDto
        {
            Token = tokenModel.Token,
            ValidTo = tokenModel.ValidTo,
            TenantId = tokenModel.TenantId,
            HasVerifiedEmail = tokenModel.HasVerifiedEmail,
        };

        public void SetOverlayCookie(long tenantId, long userId, long realRootUserId)
        {
            var days = _configuration.GetValue<int?>("RefreshToken:DurationInDays") ?? DefaultRefreshDays;
            var ticket = new ImpersonationTicket(tenantId, userId, realRootUserId, DateTimeOffset.UtcNow.AddDays(days));
            this.Response().Cookies.Append(
                ImpersonationCookieName,
                ImpersonationCookie.Protect(ticket, _jwtConfig.Key),
                this.CookieOptions(ticket.ExpiresAt));
        }

        public void ClearOverlayCookie() =>
            this.Response().Cookies.Delete(ImpersonationCookieName, this.CookieOptions(null));

        public ImpersonationTicket ReadOverlayTicket(long expectedBy)
        {
            var raw = _httpContextAccessor.HttpContext?.Request.Cookies[ImpersonationCookieName];
            var ticket = ImpersonationCookie.Unprotect(raw, _jwtConfig.Key);
            return ticket != null && ticket.By == expectedBy ? ticket : null;
        }

        private HttpResponse Response() => _httpContextAccessor.HttpContext.Response;

        // Mismas garantías que la cookie del refresh: HttpOnly, Secure, SameSite=Strict, acotada a /api/auth.
        private CookieOptions CookieOptions(DateTimeOffset? expires) => new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = CookiePath,
            Expires = expires,
        };
    }
}
