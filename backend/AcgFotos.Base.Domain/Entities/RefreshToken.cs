using System;
using AcgFotos.Core.Domain;

namespace AcgFotos.Base.Domain.Entities
{
    /// <summary>
    /// Refresh token persistido para renovación de sesión y revocación real.
    /// El valor en claro nunca se guarda: solo su SHA-256 en <see cref="TokenHash"/>.
    /// </summary>
    public class RefreshToken : MultiTenantEntityBase
    {
        /// <summary>Usuario dueño del token (FK gen_Usuarios.Id).</summary>
        public long UserId { get; set; }

        /// <summary>SHA-256 (hex) del token en claro.</summary>
        public string TokenHash { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }

        /// <summary>Fecha de revocación (rotación, logout, password, replay). Null = activo.</summary>
        public DateTime? RevokedAt { get; set; }

        /// <summary>Hash del refresh que reemplaza a éste al rotar (trazabilidad de la cadena).</summary>
        public string ReplacedByTokenHash { get; set; }

        /// <summary>
        /// Motivo de revocación: rotated | logout | password_changed | replay_detected | admin_revoke.
        /// </summary>
        public string RevokeReason { get; set; }

        /// <summary>IP del cliente que generó el token (auditoría).</summary>
        public string CreatedByIp { get; set; }

        /// <summary>User-Agent del cliente que generó el token (auditoría).</summary>
        public string CreatedByUserAgent { get; set; }

        /// <summary>True si no fue revocado y no expiró.</summary>
        public bool IsActive => RevokedAt == null && DateTime.UtcNow < ExpiresAt;
    }
}
