using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace AcgFotos.Core.AuditLog
{
    // Saneamiento de payloads que entran a gen_AuditLogs.Parametros / ResultContent.
    // Razon: el filter logueaba bodies crudos de POST, incluyendo credenciales en
    // /api/auth/token (UserName + Password en plain text).
    public static class AuditLogSanitizer
    {
        // Nombres de campos a enmascarar — case-insensitive, ingles y espanol.
        private static readonly HashSet<string> SensitiveFieldNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "Password", "Pwd", "Pass",
            "Clave", "Contrasena", "Contraseña",
            "Token", "RefreshToken", "AccessToken", "Bearer",
            "Secret", "ApiKey", "Authorization", "Cookie",
            "Key", "PrivateKey",
            "ConnectionString"
        };

        // Controllers cuyos request bodies + responses deben redactarse completos.
        private static readonly HashSet<string> SensitiveControllers = new(StringComparer.OrdinalIgnoreCase)
        {
            "Auth"
        };

        private const string RedactedValue = "***";
        private const string RedactedFullBody = "[redacted - sensitive endpoint]";

        public static bool IsSensitiveController(string controllerName)
            => !string.IsNullOrEmpty(controllerName) && SensitiveControllers.Contains(controllerName);

        // Sanitiza un body JSON: enmascara fields con nombres sensibles.
        // Si isSensitive=true (por lista central de controllers o por
        // [SensitiveEndpoint] en el action/controller), redacta todo el body.
        // Si no es JSON parseable, devuelve un placeholder seguro.
        public static string SanitizeRequestBody(string? body, bool isSensitive)
        {
            if (string.IsNullOrWhiteSpace(body))
            {
                return body ?? string.Empty;
            }

            if (isSensitive)
            {
                return RedactedFullBody;
            }

            return TrySanitizeJson(body) ?? RedactedFullBody;
        }

        // Sanitiza la respuesta serializada.
        // Si isSensitive=true: redacta. Si no: pasa el JSON ya serializado
        // por un masker de fields.
        public static string SanitizeResponse(string? serialized, bool isSensitive)
        {
            if (string.IsNullOrWhiteSpace(serialized))
            {
                return serialized ?? string.Empty;
            }

            if (isSensitive)
            {
                return RedactedFullBody;
            }

            return TrySanitizeJson(serialized) ?? serialized;
        }

        private static string? TrySanitizeJson(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                using var stream = new MemoryStream();
                using (var writer = new Utf8JsonWriter(stream))
                {
                    WriteSanitizedElement(doc.RootElement, writer);
                }
                return System.Text.Encoding.UTF8.GetString(stream.ToArray());
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static void WriteSanitizedElement(JsonElement element, Utf8JsonWriter writer, string? propertyName = null)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    writer.WriteStartObject();
                    foreach (var prop in element.EnumerateObject())
                    {
                        if (SensitiveFieldNames.Contains(prop.Name))
                        {
                            writer.WriteString(prop.Name, RedactedValue);
                        }
                        else
                        {
                            writer.WritePropertyName(prop.Name);
                            WriteSanitizedElement(prop.Value, writer, prop.Name);
                        }
                    }
                    writer.WriteEndObject();
                    break;
                case JsonValueKind.Array:
                    writer.WriteStartArray();
                    foreach (var item in element.EnumerateArray())
                    {
                        WriteSanitizedElement(item, writer);
                    }
                    writer.WriteEndArray();
                    break;
                default:
                    element.WriteTo(writer);
                    break;
            }
        }
    }
}
