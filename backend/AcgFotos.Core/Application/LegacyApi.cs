namespace AcgFotos.Core.Application
{
    /// <summary>
    /// Mensajes centralizados para APIs marcadas como <see cref="System.ObsoleteAttribute"/>
    /// pero que se conservan temporalmente porque hay consumidores sin migrar. Centralizar acá
    /// los mensajes deja en un solo lugar el inventario de deuda — al borrar el último consumidor,
    /// se borra el método obsoleto y su constante de mensaje.
    /// </summary>
    internal static class LegacyApi
    {
        public const string ExecuteEditMessage =
            "Camino legacy: merge por reflexión sobre la entidad detached. SIN consumidores en AcgFotos " +
            "(el write migró a SetValues / asignación explícita, ADR-0001). Se mantiene deprecado para " +
            "Budgeting (migración pendiente); eliminar cuando no queden consumidores.";

        public const string EntityBaseRepositoryEditMessage =
            "Camino legacy: merge por reflexión (CopyToOnlyPrimitiveValues + UpdateEntityColllection " +
            "apareando colecciones por índice). SIN consumidores en AcgFotos; reemplazar por SetValues + " +
            "asignación explícita en el AppService. Se mantiene para Budgeting; eliminar cuando no queden consumidores.";
    }
}
