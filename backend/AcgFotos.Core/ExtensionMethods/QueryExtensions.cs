using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AcgFotos.Core.ExtensionMethods
{
    public static class QueryExtensions
    {
        public enum TipoValorToWhere
        {
            varchar,
            number,
            date
        }

        public static string ToWhere(this string stringComaSeparated, string campoNombre, string operador, bool addAnd, TipoValorToWhere tipoValor)
        {

            if (string.IsNullOrEmpty(stringComaSeparated)) return string.Empty;

            var result = new StringBuilder(2000);

            var valores = stringComaSeparated.Split(char.Parse(","))?.ToList();

            for (var i = 0; i < valores.Count; i++)
            {
                if (tipoValor == TipoValorToWhere.date)
                {
                    result.Append($"   {campoNombre} {operador} TO_DATE('{valores[i]}','dd/MM/yyyy') ");
                }
                else if (tipoValor == TipoValorToWhere.number)
                {
                    result.Append($"   {campoNombre} {operador} {valores[i]} ");
                }
                else
                {
                    result.Append($"   {campoNombre} {operador} '{valores[i]}' ");
                }


                if ((i + 1) < valores.Count)
                {
                    if (addAnd)
                    {
                        result.Append(" AND ");
                    }
                    else
                    {
                        result.Append(" OR ");
                    }

                }
            }
            return result.ToString();

        }
    }
}
