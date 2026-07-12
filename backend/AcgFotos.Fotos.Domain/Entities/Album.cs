using AcgFotos.Core.Domain;

namespace AcgFotos.Fotos.Domain.Entities;

/// <summary>
/// Las fotos individuales de UN alumno: la unidad de acceso de la familia. Un código de acceso
/// abre este álbum, que ve sus fotos individuales más las grupales de su curso.
/// </summary>
public class Album : MultiTenantEntityBase
{
    public long CursoId { get; set; }
    public Curso Curso { get; set; } = null!;

    public string NombreAlumno { get; set; } = null!;

    public ICollection<CodigoAcceso> CodigosAcceso { get; set; } = new List<CodigoAcceso>();
}
