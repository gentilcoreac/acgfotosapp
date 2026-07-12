using System;
using AcgFotos.Core.Application;

namespace AcgFotos.Base.Application.Dtos
{
    /// <summary>
    /// Filtros del listado de logs (AllTenants). `SearchText` filtra por Message; además nivel y rango
    /// de fechas. Todos opcionales (null/empty deshabilita el filtro).
    /// </summary>
    public class LogInfoCriteria : ListaPaginadaCriteriaBase
    {
        public string Level { get; set; }
        public DateTime? FechaDesde { get; set; }
        public DateTime? FechaHasta { get; set; }
        /// <summary>Acota a un tenant puntual (root ve todos por defecto; este filtro es opcional).</summary>
        public long? TenantId { get; set; }
    }
}
