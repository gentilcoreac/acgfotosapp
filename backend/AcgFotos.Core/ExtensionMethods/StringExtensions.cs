using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace AcgFotos.Core.ExtensionMethods
{
    public static class StringExtensions
    {
        public static string ToPascalCase(this string valorString)
        {
            if (!string.IsNullOrEmpty(valorString))
            {
                return $"{valorString.Substring(0, 1).ToUpper()}{valorString.Substring(1)}";
            }

            return string.Empty;
        }

        public static int? ToIntOrNull(this string valorString)
        {
            if (int.TryParse(valorString, out var result)) return result;
            return null;
        }
        public static int ToIntOrCero(this string valorString)
        {
            if (int.TryParse(valorString, out var result)) return result;
            return 0;
        }

        public static DateTime StringDDMMYYYYToDate(this string fechaDDMMYYYY)
        {
            if (!DateTime.TryParseExact(fechaDDMMYYYY, "dd/MM/yyyy",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out var fecha))
            {
                throw new Exception("Parámetro definido de forma incorrecta, se esperaba formato dd/mm/yyyy");
            }
            return fecha;
        }
    }
}
