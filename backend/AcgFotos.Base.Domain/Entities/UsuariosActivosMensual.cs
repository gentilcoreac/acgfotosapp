using AcgFotos.Core.Domain;

namespace AcgFotos.Base.Domain.Entities
{
    public class UsuariosActivosMensual : MultiTenantEntityBase
    {
        public string Periodo { get; set; }
        public int Cantidad { get; set; }
    }
}
