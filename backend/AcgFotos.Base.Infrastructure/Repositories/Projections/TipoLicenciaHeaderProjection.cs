namespace AcgFotos.Base.Infrastructure.Repositories.Projections
{
    /// <summary>
    /// Shape de proyección SQL para el listado de Tipos de licencia: id/código/descripción/default.
    /// La proyección ocurre en SQL → no trae las colecciones (roles/usuarios/licencias de tenant).
    /// El AppService la mapea a TipoLicenciaHeaderDto (Application) — Infrastructure no referencia
    /// Application.
    /// </summary>
    public class TipoLicenciaHeaderProjection
    {
        public long Id { get; set; }
        public string CodigoTipoLicencia { get; set; }
        public string Descripcion { get; set; }
        public bool EsDefaultParaNuevoTenant { get; set; }
    }
}
