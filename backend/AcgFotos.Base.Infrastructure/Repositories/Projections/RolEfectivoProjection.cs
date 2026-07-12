namespace AcgFotos.Base.Infrastructure.Repositories.Projections
{
    /// <summary>
    /// Proyección de un rol EFECTIVO de un usuario para mostrarlo (read-only) con su origen:
    /// resuelta sobre la vista <c>vw_UsuarioRolesEfectivos</c> + el nombre del rol y, cuando el
    /// origen es un grupo, el nombre del grupo. Un mismo rol puede aparecer varias veces (directo
    /// y/o por uno o más grupos): cada fila es un origen distinto.
    /// </summary>
    public class RolEfectivoProjection
    {
        public long RolId { get; set; }
        public string RolDescripcion { get; set; }
        /// <summary>"Directo" o "Grupo".</summary>
        public string Origen { get; set; }
        public long? GrupoId { get; set; }
        /// <summary>Nombre del grupo del que se hereda el rol; null si el rol es directo.</summary>
        public string GrupoNombre { get; set; }
        /// <summary>`true` si el rol está en la licencia activa del usuario; si es `false`, está BLOQUEADO por licencia.</summary>
        public bool PermitidoPorLicencia { get; set; }
    }
}
