using System.Collections.Generic;

namespace AcgFotos.Api.IntegrationTests.Infrastructure
{
    /// <summary>
    /// Wrapper mínimo del <c>PaginationSet&lt;T&gt;</c> de la API para deserializar listados paginados en los
    /// tests. Genérico → una sola fuente de la forma de respuesta (Items + TotalCount); se parametriza con el
    /// DTO real del área (RolHeaderDto, AplicacionDto, …), así un rename de Items/TotalCount se caza en un solo lugar.
    /// </summary>
    public sealed record Page<T>(List<T> Items, int TotalCount);
}
