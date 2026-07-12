using AcgFotos.Core.Domain;

namespace AcgFotos.Base.Domain.Entities
{
    /// <summary>
    /// Versión GLOBAL de autorización del sistema (tabla <c>gen_AuthzVersion</c>, 1 sola fila).
    /// Es un único contador que se incrementa cada vez que se guarda CUALQUIER entidad que pueda
    /// alterar los permisos efectivos de algún usuario (roles/permisos/endpoints/menús/grupos/licencias
    /// — ver el set en <c>AcgFotosDbContext</c>). La clave del caché de endpoints permitidos
    /// (<c>EndpointAuthoritation</c>) incluye este número: al subir la versión, las claves viejas quedan
    /// huérfanas y la próxima request recomputa con el cambio — frescura al instante, sin recomputar si
    /// nada cambió (reemplaza el TTL absoluto de 30s). Ver ADR-0003 §6.3.
    ///
    /// NO es multi-tenant (es un número del sistema, compartido por todos los workers y persistido en DB
    /// → sobrevive reinicios/scale-out de App Service). Por eso NO hereda de <c>MultiTenantEntityBase</c>:
    /// queda fuera del filtro global de tenant.
    /// </summary>
    public class AuthzVersion : EntityBase
    {
        public long Version { get; set; }
    }
}
