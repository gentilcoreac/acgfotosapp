using System;

namespace AcgFotos.Core.Exceptions
{
    /// <summary>
    /// Operación rechazada por una regla de autorización de negocio — distinta del catálogo
    /// permiso→endpoint de <c>EndpointAuthoritation</c> (que depende de <c>AuthorizationEnabled</c>).
    /// Pensada para guards explícitos dentro de un AppService que deben aplicar SIEMPRE, más allá de
    /// ese flag (p. ej. un vertical rechazando una sesión que no es la suya). Se mapea a 403 en
    /// <see cref="ExceptionHandlingMiddleware"/>.
    /// </summary>
    public class ForbiddenException : Exception
    {
        public ForbiddenException(string message) : base(message)
        {
        }
    }
}
