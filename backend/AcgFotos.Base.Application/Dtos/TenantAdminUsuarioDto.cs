namespace AcgFotos.Base.Application.Dtos
{
    /// <summary>
    /// Proyección <b>solo lectura</b> del/los administrador(es) de un tenant (usuarios con el rol
    /// default de nuevo tenant). Se expone en <see cref="TenantDto"/> para mostrarlo en el edit del
    /// ABM; no viaja en el input ni se persiste. Lo puebla el AppService en GetByIdAsync.
    /// </summary>
    public class TenantAdminUsuarioDto
    {
        /// <summary>Id del usuario admin. Lo usa el selector de impersonalización (ADR-0002) como target.</summary>
        public long Id { get; set; }

        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
    }
}
