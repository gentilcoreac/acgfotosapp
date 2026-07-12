using System;
using System.Threading.Tasks;

namespace AcgFotos.Base.Application.IServices
{
    /// <summary>Refresh token emitido: valor en claro (entregado una sola vez) + expiración.</summary>
    public record IssuedRefreshToken(string Token, DateTime ExpiresAt);

    /// <summary>Resultado de validar un refresh token recibido en /auth/refresh.</summary>
    public class RefreshValidationResult
    {
        public bool IsValid { get; init; }
        public long UserId { get; init; }
        public long TenantId { get; init; }

        public static RefreshValidationResult Invalid() => new() { IsValid = false };

        public static RefreshValidationResult Valid(long userId, long tenantId) =>
            new() { IsValid = true, UserId = userId, TenantId = tenantId };
    }

    /// <summary>Motivos de revocación de refresh tokens (columna RevokeReason).</summary>
    public static class RefreshTokenRevokeReasons
    {
        public const string Rotated = "rotated";
        public const string Logout = "logout";
        public const string PasswordChanged = "password_changed";
        public const string ReplayDetected = "replay_detected";
        public const string AdminRevoke = "admin_revoke";
    }

    /// <summary>
    /// Ciclo de vida de los refresh tokens (emisión, validación, rotación y revocación).
    /// No maneja cookies — eso lo hace el controller. Ver Docs/design-refresh-tokens.md.
    /// </summary>
    public interface IRefreshTokenService
    {
        /// <summary>Emite y persiste un refresh token nuevo para el usuario (login).</summary>
        Task<IssuedRefreshToken> IssueAsync(long userId, long tenantId);

        /// <summary>
        /// Valida el refresh recibido. Si está revocado por rotación (replay), revoca toda la
        /// cadena activa del usuario. Devuelve IsValid=false ante cualquier problema.
        /// </summary>
        Task<RefreshValidationResult> ValidateAsync(string rawToken);

        /// <summary>Rota: revoca el refresh actual y emite uno nuevo (post-validación).</summary>
        Task<IssuedRefreshToken> RotateAsync(string rawToken, long userId, long tenantId);

        /// <summary>Revoca un refresh puntual (logout).</summary>
        Task RevokeAsync(string rawToken, string reason);

        /// <summary>Revoca todos los refresh activos del usuario (cambio de password, etc.).</summary>
        Task RevokeAllForUserAsync(long userId, string reason);
    }
}
