namespace AcgFotos.Base.Domain.Entities
{
    /// <summary>
    /// Fila de la vista <c>vw_UsuarioRolesEfectivos</c>: un rol que el usuario tiene de forma
    /// EFECTIVA, ya sea asignado directamente (<see cref="Origen"/> = "Directo") o heredado de uno de
    /// sus grupos ("Grupo"). Es la <b>fuente única de verdad</b> de la unión "roles directos ∪ roles
    /// de grupos" que consumen tanto la autorización de endpoints como el armado del menú.
    ///
    /// Es un modelo de SOLO LECTURA mapeado a una vista (keyless, <c>ToView</c>): no se inserta ni
    /// trackea, y no genera tabla en las migraciones (la vista la crea el SQL de la migración).
    /// Para los roles efectivos "a secas" los consumidores hacen <c>DISTINCT RolId</c>; las columnas
    /// <see cref="Origen"/>/<see cref="GrupoId"/> habilitan, a futuro, la visualización por origen.
    /// </summary>
    public class UsuarioRolEfectivo
    {
        public long UsuarioId { get; set; }
        public long TenantId { get; set; }
        public long RolId { get; set; }
        /// <summary>"Directo" (gen_UsuarioRoles) o "Grupo" (gen_UsuarioGrupos → gen_GrupoRoles).</summary>
        public string Origen { get; set; }
        /// <summary>Grupo del que se hereda el rol; null cuando el rol es directo.</summary>
        public long? GrupoId { get; set; }
        /// <summary>
        /// `true` si el rol está incluido en la **licencia activa** del usuario. La licencia es TOPE
        /// DURO: los consumidores (menú/autorización) sólo consideran efectivos los roles con este
        /// flag en `true` — un usuario nunca tiene efectivo un rol fuera de su licencia, venga directo
        /// o por grupo. La visualización muestra todos y marca los bloqueados. (Root no pasa por acá.)
        /// </summary>
        public bool PermitidoPorLicencia { get; set; }
    }
}
