using AcgFotos.Core.Domain;

namespace AcgFotos.Fotos.Domain.Entities;

/// <summary>
/// Marca de agua propia del estudio (ADR-15): de 1 a 3 capas ya rasterizadas por el front, que la
/// API sólo compone sobre la foto. Un evento sin perfil propio usa el <see cref="EsDefault"/> del
/// tenant; sin ninguno cargado, la cascada cae en <c>OpcionesFotos</c> (comportamiento actual).
/// </summary>
public class PerfilMarcaAgua : MultiTenantEntityBase
{
    public string Nombre { get; set; } = null!;

    /// <summary>A lo sumo un perfil default por tenant.</summary>
    public bool EsDefault { get; set; }

    /// <summary>Si además de preview se marca el thumbnail (decisión sobre la marca, no sobre la resolución).</summary>
    public bool MarcarThumb { get; set; } = true;

    public ICollection<CapaMarcaAgua> Capas { get; set; } = new List<CapaMarcaAgua>();
}
