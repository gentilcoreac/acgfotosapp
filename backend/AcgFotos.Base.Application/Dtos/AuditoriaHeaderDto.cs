using System;
using AcgFotos.Core.Application;

namespace AcgFotos.Base.Application.Dtos
{
    /// <summary>
    /// Proyección liviana para el LISTADO de auditoría: solo lo que muestra la grilla. Excluye los
    /// campos pesados (Parametros, ResultContent) y los de detalle (ClientIP/UserAgent), que se traen
    /// por id en <see cref="AuditoriaDto"/>. Evita transferir nvarchar(max) por cada fila de la página.
    /// </summary>
    public class AuditoriaHeaderDto : DtoBase
    {
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
