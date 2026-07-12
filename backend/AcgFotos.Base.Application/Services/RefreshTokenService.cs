using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using AcgFotos.Base.Application.IServices;
using AcgFotos.Base.Domain.Entities;
using AcgFotos.Base.Infrastructure.Repositories;
using AcgFotos.Core.Data;
using AcgFotos.Core.Security;

namespace AcgFotos.Base.Application.Services
{
    public class RefreshTokenService : IRefreshTokenService
    {
        private const int DefaultDurationInDays = 14;

        private readonly IRefreshTokenRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public RefreshTokenService(
            IRefreshTokenRepository repository,
            IUnitOfWork unitOfWork,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
        }

        private int DurationInDays =>
            _configuration.GetValue<int?>("RefreshToken:DurationInDays") ?? DefaultDurationInDays;

        private const int DefaultReuseGraceSeconds = 10;

        /// <summary>
        /// Ventana (segundos) tras rotar un refresh en la que reusar el token viejo se trata como
        /// benigno (F5 rápido / carrera), no como replay. Configurable por <c>RefreshToken:ReuseGraceSeconds</c>.
        /// </summary>
        private int ReuseGraceSeconds =>
            _configuration.GetValue<int?>("RefreshToken:ReuseGraceSeconds") ?? DefaultReuseGraceSeconds;

        public async Task<IssuedRefreshToken> IssueAsync(long userId, long tenantId)
        {
            var (raw, entity) = this.BuildToken(userId, tenantId);
            _repository.Add(entity);
            await _unitOfWork.CommitAsync();
            return new IssuedRefreshToken(raw, entity.ExpiresAt);
        }

        public async Task<RefreshValidationResult> ValidateAsync(string rawToken)
        {
            var token = await _repository.GetByHashAsync(RefreshTokenCrypto.Hash(rawToken));
            if (token == null)
            {
                return RefreshValidationResult.Invalid();
            }

            if (token.RevokedAt != null)
            {
                if (token.RevokeReason == RefreshTokenRevokeReasons.Rotated)
                {
                    // Ventana de gracia: un F5 muy rápido (o una carrera de refresh) reusa el token recién
                    // rotado antes de que asiente la cookie nueva. Dentro de la ventana lo tratamos como
                    // benigno (misma sesión del mismo usuario) en vez de declarar replay y cortar TODA la
                    // cadena — con 401 el front pateaba a login. El controller emite un refresh nuevo
                    // (RotateAsync sobre un token ya revocado no lo re-revoca, solo crea el sucesor).
                    if (DateTime.UtcNow <= token.RevokedAt.Value.AddSeconds(this.ReuseGraceSeconds))
                    {
                        return RefreshValidationResult.Valid(token.UserId, token.TenantId);
                    }

                    // Fuera de la ventana = replay real (token robado reusado tarde) → cortar la cadena.
                    await _repository.RevokeAllActiveForUserAsync(token.UserId, RefreshTokenRevokeReasons.ReplayDetected);
                }
                return RefreshValidationResult.Invalid();
            }

            if (DateTime.UtcNow >= token.ExpiresAt)
            {
                return RefreshValidationResult.Invalid();
            }

            return RefreshValidationResult.Valid(token.UserId, token.TenantId);
        }

        public async Task<IssuedRefreshToken> RotateAsync(string rawToken, long userId, long tenantId)
        {
            var old = await _repository.GetByHashAsync(RefreshTokenCrypto.Hash(rawToken));
            var (raw, newToken) = this.BuildToken(userId, tenantId);
            _repository.Add(newToken);

            if (old != null && old.RevokedAt == null)
            {
                old.RevokedAt = DateTime.UtcNow;
                old.RevokeReason = RefreshTokenRevokeReasons.Rotated;
                // El hash del nuevo se conoce antes del insert → link forense en un único commit.
                old.ReplacedByTokenHash = newToken.TokenHash;
            }

            await _unitOfWork.CommitAsync();

            return new IssuedRefreshToken(raw, newToken.ExpiresAt);
        }

        public async Task RevokeAsync(string rawToken, string reason)
        {
            var token = await _repository.GetByHashAsync(RefreshTokenCrypto.Hash(rawToken));
            if (token != null && token.RevokedAt == null)
            {
                token.RevokedAt = DateTime.UtcNow;
                token.RevokeReason = reason;
                await _unitOfWork.CommitAsync();
            }
        }

        public Task RevokeAllForUserAsync(long userId, string reason) =>
            _repository.RevokeAllActiveForUserAsync(userId, reason);

        private (string raw, RefreshToken entity) BuildToken(long userId, long tenantId)
        {
            var raw = RefreshTokenCrypto.GenerateRawToken();
            var connection = _httpContextAccessor.HttpContext?.Connection;
            var request = _httpContextAccessor.HttpContext?.Request;

            var entity = new RefreshToken
            {
                UserId = userId,
                TenantId = tenantId,
                TokenHash = RefreshTokenCrypto.Hash(raw),
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(this.DurationInDays),
                CreatedByIp = connection?.RemoteIpAddress?.ToString(),
                CreatedByUserAgent = request?.Headers.UserAgent.ToString(),
            };
            return (raw, entity);
        }
    }
}
