using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;

namespace AcgFotos.Core.Security
{
    public class JwtSecurityTokenFactory : IJwtSecurityTokenFactory
    {
        private readonly JwtSecurityTokenConfig _jwtConfig;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public JwtSecurityTokenFactory(
            IOptions<JwtSecurityTokenConfig> jwt,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor)
        {
            _jwtConfig = jwt.Value;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
        }

        public TokenModel CreateInstance(long userId, string userName, string email, long tenantId, bool isAdmin, string securityStamp, long? impersonatedBy = null)
        {
            // IP real del cliente que solicita el token (vía RemoteIpAddress + ForwardedHeaders).
            // Se guarda como claim "ip" para trazabilidad.
            var ipAddress = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
            var rootTenantId = _configuration.GetValue<long>("RootTenantId");

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, userName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim("userId", userId.ToString()),
                new Claim("ip", ipAddress),
                new Claim("tenant", tenantId.ToString()),
                new Claim("isRoot", (tenantId == rootTenantId).ToString()),
                new Claim("isAdmin", isAdmin.ToString()),
                new Claim("securityStamp", securityStamp) //util para invalidar tokens cuando cambia psw. Es un valor random/opaco, sin valor para un atacante.
            };

            // Token de impersonalización (ADR-0002): el token va scopeado al usuario destino (claims de
            // arriba) y este claim firmado preserva el userId real de root que está impersonando (auditoría).
            if (impersonatedBy.HasValue)
            {
                claims.Add(new Claim("impersonatedBy", impersonatedBy.Value.ToString()));
            }

            var symmetricSecurityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtConfig.Key));
            var signingCredentials = new SigningCredentials(symmetricSecurityKey, SecurityAlgorithms.HmacSha256);

            var jwtSecurityToken = new JwtSecurityToken(
                issuer: _jwtConfig.Issuer,
                audience: _jwtConfig.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwtConfig.DurationInMinutes),
                signingCredentials: signingCredentials);

            var tokenModel = new TokenModel()
            {
                TenantId = tenantId,
                HasVerifiedEmail = true,
                ValidTo = jwtSecurityToken.ValidTo,
                Token = new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken),
                IsRoot = tenantId == rootTenantId
            };
            return tokenModel;
        }
    }
}
