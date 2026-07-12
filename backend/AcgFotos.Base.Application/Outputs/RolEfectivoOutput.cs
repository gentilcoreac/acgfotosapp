namespace AcgFotos.Base.Application.Outputs
{
    /// <summary>
    /// Rol EFECTIVO de un usuario (read-only), con su origen, para la visualización en el front:
    /// roles asignados directamente + heredados de los grupos del usuario. Un mismo rol puede venir
    /// repetido con distinto <see cref="Origen"/> (directo y/o por varios grupos).
    /// </summary>
    public class RolEfectivoOutput
    {
        public long RolId { get; set; }
        public string RolDescripcion { get; set; }
        /// <summary>"Directo" o "Grupo".</summary>
        public string Origen { get; set; }
        public long? GrupoId { get; set; }
        /// <summary>Nombre del grupo del que se hereda el rol; null si el rol es directo.</summary>
        public string GrupoNombre { get; set; }
        /// <summary>`true` si el rol está en la licencia activa del usuario; `false` = BLOQUEADO por licencia.</summary>
        public bool PermitidoPorLicencia { get; set; }
    }
}
