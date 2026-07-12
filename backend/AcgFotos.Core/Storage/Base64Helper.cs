using System;

namespace AcgFotos.Core.Storage
{
    public static class Base64Helper
    {
        /// <summary>
        /// Extrae el base64 puro de un data-URI ("data:image/png;base64,XXXX" → "XXXX").
        /// Si no tiene el prefijo data-URI, devuelve el string tal cual.
        /// </summary>
        public static string Clean(string base64String)
        {
            if (string.IsNullOrEmpty(base64String))
            {
                return base64String;
            }

            var splited = base64String.Split(new[] { "base64," }, StringSplitOptions.None);
            return splited.Length > 1 ? splited[1] : base64String;
        }
    }
}
