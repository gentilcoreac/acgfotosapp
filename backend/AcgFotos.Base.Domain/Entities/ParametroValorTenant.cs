using AcgFotos.Core.Domain;

namespace AcgFotos.Base.Domain.Entities
{
    // NO es IMultiTenantEntityBase a proposito: ROOT administra los overrides de TODOS los tenants desde
    // una pantalla root-only centralizada (elige tenant y pasa TenantId en el body/param, sin impersonar;
    // mismo modelo que aplicaciones-tenant-id). El filtro global de tenant + el guard de escritura
    // bloquearian ese flujo de root. El aislamiento para NO-root (que no debe ver/tocar otros tenants) se
    // hace explicito en ParametroValorTenantAppService/ParametroRepository (scoping por contexto cuando
    // !IsRoot). Ver hallazgo #12 en sections/210-pvt.md.
    public class ParametroValorTenant : EntityBase
    {
        public string Valor { get; set; }
        public long TenantId { get; set; }
        public long ParametroId { get; set; }
        public Parametro Parametro { get; set; }
    }
}
