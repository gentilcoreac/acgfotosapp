using AcgFotos.Core.Domain;

namespace AcgFotos.Fotos.Domain.Entities;

/// <summary>
/// Resolución y calidad de los derivados, como eje independiente de <see cref="PerfilMarcaAgua"/>
/// (ADR-15 §5): se puede querer marca sutil con resolución baja o al revés. Un evento sin opciones
/// propias usa el <see cref="EsDefault"/> del tenant; sin ninguna cargada, la cascada cae en
/// <c>OpcionesFotos</c> (comportamiento actual).
/// </summary>
public class OpcionesPublicacion : MultiTenantEntityBase
{
    public string Nombre { get; set; } = null!;

    /// <summary>A lo sumo un conjunto default por tenant.</summary>
    public bool EsDefault { get; set; }

    public int LadoMayorPreview { get; set; }
    public int LadoMayorThumb { get; set; }

    /// <summary>Calidad WebP (0-100).</summary>
    public int Calidad { get; set; }
}
