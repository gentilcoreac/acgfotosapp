using System;

namespace AcgFotos.Base.Infrastructure.Repositories.Projections
{
    /// <summary>
    /// Proyección SQL del listado de auditoría: solo las columnas que muestra la grilla. Evita
    /// traer de la base los campos pesados (Parametros, ResultContent nvarchar(max)) por cada fila.
    /// El detalle completo se obtiene por id.
    /// </summary>
    public class AuditoriaHeaderProjection
    {
        public long Id { get; set; }
        public DateTime FechaHora { get; set; }
        public double Duracion { get; set; }
        public string Servicio { get; set; }
        public string Metodo { get; set; }
        public long? UsuarioId { get; set; }
        public long? ImpersonatedBy { get; set; }
        public string HttpMethod { get; set; }
        public string RequestAbsolutePath { get; set; }
        public string ResultStatusCode { get; set; }
        public string UsuarioNombre { get; set; }
    }
}
