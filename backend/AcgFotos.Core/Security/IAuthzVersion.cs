namespace AcgFotos.Core.Security
{
    /// <summary>
    /// Read-side de la versión GLOBAL de autorización (tabla <c>gen_AuthzVersion</c>, 1 fila). La consume
    /// <see cref="EndpointAuthoritation"/> para versionar la clave del caché de endpoints permitidos.
    ///
    /// El INCREMENTO ("bump") NO vive acá: lo hace <c>AcgFotosDbContext.SaveChanges</c> de forma
    /// transaccional cuando detecta cambios en entidades de autorización (ver ADR-0003 §6.3), así el bump
    /// viaja en la misma transacción que el cambio que lo dispara y no puede olvidarse en ningún AppService.
    /// Esta interfaz, en cambio, vive en Core (sin EF) porque el filtro de autorización corre acá y lee la
    /// versión por SQL crudo, igual que <see cref="ISecurityRepository"/>.
    /// </summary>
    public interface IAuthzVersion
    {
        /// <summary>Versión actual (leída en vivo de la DB: 1 fila por PK, sub-ms, queda en buffer).</summary>
        long Get();
    }
}
