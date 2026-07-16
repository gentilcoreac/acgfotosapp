using AcgFotos.Core.Domain;

namespace AcgFotos.Fotos.Domain.Entities;

/// <summary>
/// Código que la familia canjea para entrar a SU participante (sin registro, ADR-02). Corto y legible
/// (se reparte como tarjeta/QR) pero con entropía suficiente; el canje está rate-limiteado.
/// Puede haber más de uno por participante (reemplazo de un código comprometido sin perder el histórico).
/// </summary>
public class CodigoAcceso : MultiTenantEntityBase
{
    public long ParticipanteId { get; set; }
    public Participante Participante { get; set; } = null!;

    /// <summary>Ej. "K7F3-9QMD". Único por tenant.</summary>
    public string Codigo { get; set; } = null!;

    public bool Activo { get; set; } = true;

    public DateTime CreadoEn { get; set; }
}
