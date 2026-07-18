using System;

namespace AcgFotos.Core.Security
{
    /// <summary>
    /// Marca un endpoint como accesible por una sesión de familia (ADR-11: JWT con
    /// <c>sessionType=familia</c>, sin usuario de plataforma). <see cref="EndpointAuthoritation"/> exige
    /// esta marca explícita para esas sesiones: la matriz de permisos (gen_Usuarios/roles) no tiene
    /// ninguna fila para ellas, así que sin este allowlist quedarían 403 en TODO endpoint autenticado —
    /// y sin el allowlist, lo contrario (bypasear la matriz para "familia") dejaría a esas sesiones
    /// entrar a cualquier endpoint admin. Autorización explícita, no bypass.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class AllowFamiliaSessionAttribute : Attribute
    {
    }
}
