using AcgFotos.Core.Domain;

namespace AcgFotos.Fotos.Domain.Entities;

/// <summary>
/// Las fotos individuales de UN participante: la unidad de acceso de la familia. Un código de acceso
/// abre este participante, que ve sus fotos individuales más las grupales de su grupo.
/// </summary>
public class Participante : MultiTenantEntityBase
{
    public long GrupoId { get; set; }
    public Grupo Grupo { get; set; } = null!;

    public string Nombre { get; set; } = null!;

    public ICollection<CodigoAcceso> CodigosAcceso { get; set; } = new List<CodigoAcceso>();
}
