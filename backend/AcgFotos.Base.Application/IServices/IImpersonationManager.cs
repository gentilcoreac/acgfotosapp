using AcgFotos.Core.Security;
using AcgFotos.Core.Session;

namespace AcgFotos.Base.Application.IServices
{
    /// <summary>
    /// Lógica compartida de impersonalización (ADR-0002) usada por el <c>ImpersonationController</c> (alta/baja
    /// de impersonalización + listado) y por <c>AuthController.Refresh</c> (el "hook" que sostiene la
    /// impersonalización a través de los refresh). Centralizada acá para no duplicarla entre controllers.
    /// El armado del token sigue en <see cref="IJwtSecurityTokenFactory"/>; acá va el resto: resolver al
    /// root real, mapear el DTO y manejar la cookie-overlay firmada.
    /// </summary>
    public interface IImpersonationManager
    {
        /// <summary>
        /// userId real de root del contexto actual: el logueado si es root, el <c>impersonatedBy</c> si ya
        /// está impersonando (puede re-impersonar para cambiar de destino), o <c>null</c> si no puede impersonar.
        /// </summary>
        long? ResolveRealRootUserId();

        /// <summary>Mapea el <see cref="TokenModel"/> al DTO de respuesta (mismo shape que <c>/auth/token</c>).</summary>
        TokenModelDto ToDto(TokenModel tokenModel);

        /// <summary>Setea la cookie-overlay firmada que sostiene la impersonalización a través de los refresh.</summary>
        void SetOverlayCookie(long tenantId, long userId, long realRootUserId);

        /// <summary>Borra la cookie-overlay (stop / logout).</summary>
        void ClearOverlayCookie();

        /// <summary>
        /// Lee y valida la cookie-overlay del request. Devuelve <c>null</c> si no hay, está manipulada/expirada
        /// o el <c>by</c> no coincide con <paramref name="expectedBy"/> (no se puede usar la overlay de otro).
        /// </summary>
        ImpersonationTicket ReadOverlayTicket(long expectedBy);
    }
}
