using System;

namespace AcgFotos.Core.AuditLog
{
    // Marca un controller o accion como sensible. El AuditLogFilter
    // redacta request body + response cuando el endpoint esta marcado.
    //
    // Coexiste con AuditLogSanitizer.SensitiveControllers (lista central):
    //   - Lista central: safety net por nombre de controller.
    //   - Atributo: explicito por endpoint, util cuando solo algunos metodos
    //     de un controller son sensibles (ej. un UsuarioController donde
    //     solo /cambiar-password lo es).
    //
    // Ambos mecanismos se evaluan con OR: si cualquiera matchea, se redacta.
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class SensitiveEndpointAttribute : Attribute
    {
    }
}
