using AcgFotos.Core.Exceptions;
using AcgFotos.Core.Localization.APIResources;
using AcgFotos.Core.Session;

namespace AcgFotos.Fotos.Application.Security;

/// <summary>
/// Defensa en profundidad (2026-07-18): <c>EndpointAuthoritation</c> ya restringe una sesión de
/// familia (ADR-11) a los endpoints marcados <c>[AllowFamiliaSession]</c>, pero esa matriz solo corre
/// si <c>AuthorizationEnabled=true</c> — y hoy el default de la app (dev y `appsettings.json`) es
/// <c>false</c>. Mientras eso siga así, los AppServices ADMIN del vertical (Evento, Grupo, Foto) deben
/// rechazar explícitamente una sesión de familia por su cuenta: sin este guard, un JWT de familia
/// recién canjeado podría, por ejemplo, borrar la foto de OTRO participante vía el endpoint admin
/// (verificado con un smoke test manual — ver docs/05-notas-abiertas.md).
/// </summary>
internal static class FamiliaSessionGuard
{
    public static void EnsureNoFamiliaSession(IAppContext appContext)
    {
        if (appContext.IsFamiliaSession)
        {
            throw new ForbiddenException(MessagesAPI.ErrorUserNotPrivilegesAccess);
        }
    }
}
