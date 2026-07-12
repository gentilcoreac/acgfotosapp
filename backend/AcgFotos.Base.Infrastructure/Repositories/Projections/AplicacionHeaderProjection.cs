namespace AcgFotos.Base.Infrastructure.Repositories.Projections
{
    /// <summary>
    /// Shape de proyección SQL para el listado de Aplicaciones: sólo las columnas que muestra la
    /// grilla (id/código/nombre/activo). La proyección ocurre en SQL → no trae Icono/IconoUrl ni las
    /// colecciones. El AppService la mapea a AplicacionDto (Application) — Infrastructure no referencia
    /// Application.
    /// </summary>
    public class AplicacionHeaderProjection
    {
        public long Id { get; set; }
        public string Codigo { get; set; }
        public string Nombre { get; set; }
        public bool Activo { get; set; }
    }
}
