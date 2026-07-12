using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace AcgFotos.Core.Controllers
{
    /// <summary>
    /// Forma estándar del cuerpo de error de la API: <c>{ message, errors, traceId? }</c>. Tipo con
    /// nombre (no anónimo) para que el contrato sea explícito y Swagger lo documente vía
    /// <c>[ProducesResponseType(typeof(ApiErrorResponse), 400)]</c>.
    /// <list type="bullet">
    ///   <item><c>message</c>: titular del error, una línea, siempre presente.</item>
    ///   <item><c>errors</c>: detalle accionable (p. ej. varias reglas de validación). Vacío si no hay; nunca incluye el titular.</item>
    ///   <item><c>traceId</c>: id de correlación para soporte (solo errores técnicos). Se omite del JSON si es null.</item>
    /// </list>
    /// Los nombres JSON se fijan con <see cref="JsonPropertyNameAttribute"/> → camelCase garantizado
    /// tanto en MVC como en el <c>JsonSerializer.Serialize</c> manual del middleware, sin depender de
    /// la <c>PropertyNamingPolicy</c> del serializador.
    /// </summary>
    public sealed record ApiErrorResponse
    {
        /// <summary>Titular del error (una línea). Siempre presente.</summary>
        [JsonPropertyName("message")]
        public string Message { get; init; } = string.Empty;

        /// <summary>Detalle accionable (validaciones de negocio). Vacío si no hay; nunca incluye el titular.</summary>
        [JsonPropertyName("errors")]
        public string[] Errors { get; init; } = Array.Empty<string>();

        /// <summary>Id de correlación para soporte (errores técnicos). Se omite del JSON si es null.</summary>
        [JsonPropertyName("traceId")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string TraceId { get; init; }

        /// <summary>Un titular y, opcionalmente, una lista de detalle (se le quita el titular para no repetirlo).</summary>
        public static ApiErrorResponse From(string message, IEnumerable<string> errors = null)
        {
            var head = message ?? string.Empty;
            return new ApiErrorResponse
            {
                Message = head,
                Errors = Clean(errors).Where(d => d != head).ToArray(),
            };
        }

        /// <summary>
        /// A partir de una lista de mensajes (p. ej. errores de Identity/ModelState): el primero es el
        /// titular y el resto va como detalle. Lista vacía → titular vacío y sin detalle.
        /// </summary>
        public static ApiErrorResponse FromMany(IEnumerable<string> messages)
        {
            var arr = Clean(messages);
            return new ApiErrorResponse
            {
                Message = arr.Length > 0 ? arr[0] : string.Empty,
                Errors = arr.Length > 1 ? arr.Skip(1).ToArray() : Array.Empty<string>(),
            };
        }

        /// <summary>Error técnico: titular amable + traceId de correlación (sin detalle accionable).</summary>
        public static ApiErrorResponse Technical(string message, string traceId)
            => new ApiErrorResponse { Message = message ?? string.Empty, TraceId = traceId };

        private static string[] Clean(IEnumerable<string> values)
            => (values ?? Enumerable.Empty<string>())
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .ToArray();
    }
}
