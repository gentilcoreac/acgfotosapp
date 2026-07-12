namespace AcgFotos.Base.Infrastructure.Repositories.Projections
{
    /// <summary>
    /// Conteo mensual agregado (año/mes → cantidad). Genérico: lo usan tanto la consulta de usuarios
    /// activos como la de altas. El relleno de meses faltantes y el formato lo hace el AppService.
    /// </summary>
    public class ActividadMensualProjection
    {
        public int Anio { get; set; }

        public int Mes { get; set; }

        public int Cantidad { get; set; }
    }
}
