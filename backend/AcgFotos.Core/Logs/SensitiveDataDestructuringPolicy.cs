using System;
using System.Collections.Generic;
using Serilog.Core;
using Serilog.Events;

namespace AcgFotos.Core.Logs
{
    // Destructurer que enmascara properties con nombres sensibles cuando un objeto se
    // loguea estructuradamente con Serilog (`Log.Information("{@User}", user)`).
    // Defense-in-depth: aun si alguien hace `_logger.LogInformation("{@req}", request)`
    // con un DTO que tenga Password, el valor se reemplaza por "***".
    public class SensitiveDataDestructuringPolicy : IDestructuringPolicy
    {
        private static readonly HashSet<string> SensitiveFieldNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "Password", "Pwd", "Pass",
            "Clave", "Contrasena", "Contraseña",
            "Token", "RefreshToken", "AccessToken", "Bearer",
            "Secret", "ApiKey", "Authorization", "Cookie",
            "Key", "PrivateKey",
            "ConnectionString"
        };

        private const string RedactedValue = "***";

        public bool TryDestructure(object value, ILogEventPropertyValueFactory propertyValueFactory, out LogEventPropertyValue? result)
        {
            if (value == null)
            {
                result = null;
                return false;
            }

            var type = value.GetType();
            if (!type.IsClass || type == typeof(string))
            {
                result = null;
                return false;
            }

            var props = type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            var hasSensitive = false;
            foreach (var p in props)
            {
                if (SensitiveFieldNames.Contains(p.Name))
                {
                    hasSensitive = true;
                    break;
                }
            }
            if (!hasSensitive)
            {
                result = null;
                return false;
            }

            var properties = new List<LogEventProperty>();
            foreach (var p in props)
            {
                if (!p.CanRead) continue;
                object? propValue;
                try
                {
                    propValue = p.GetValue(value);
                }
                catch
                {
                    continue;
                }

                if (SensitiveFieldNames.Contains(p.Name))
                {
                    properties.Add(new LogEventProperty(p.Name, new ScalarValue(RedactedValue)));
                }
                else
                {
                    properties.Add(new LogEventProperty(p.Name, propertyValueFactory.CreatePropertyValue(propValue, destructureObjects: true)));
                }
            }

            result = new StructureValue(properties, type.Name);
            return true;
        }
    }
}
