using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AcgFotos.Core.Compressed
{
    public static class Compressed
    {
        private static readonly JsonSerializerOptions s_NamedFloatingPointLiterals = new()
        {  //Para evitar errores en measures DAX enviamos "0NaN" (ej: divisón por cero en una measure devuelve NaN)
            NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals
        };


        /// <summary>
        /// Descomprime un string comprimido con GZIP
        /// </summary>
        /// <param name="compressedData">String comprimido.</param>
        /// <returns>Retorna Listado de tipo diccionario.</returns>
        public static List<IDictionary<string, object>> DecompressToList(string compressedData)
        {
            var bytes = Convert.FromBase64String(compressedData);
            var options = new JsonSerializerOptions();
            options.Converters.Add(new ObjectNativeTypeConverter());

            return JsonSerializer.Deserialize<List<IDictionary<string, object>>>(bytes.UnZip(), options);
        }

        /// <summary>
        /// Descomprime un string comprimido con GZIP
        /// </summary>
        /// <param name="compressedData">String comprimido.</param>
        /// <returns>Retorna string.</returns>
        public static string DecompressToString(string compressedData)
        {
            var bytes = Convert.FromBase64String(compressedData);

            return JsonSerializer.Deserialize<string>(bytes.UnZip());
        }

        /// <summary>
        /// Comprime un string con GZIP
        /// </summary>
        /// <param name="data">datos a comprimir de tipo string.</param>
        /// <returns>Retorna string.</returns>
        public static string CompressTo(string data)
        {
            var bytes = Encoding.UTF8.GetBytes(data);

            return JsonSerializer.Serialize(bytes.Zip());
        }

        /// <summary>
        /// Comprime un listado tipo diccionario con GZIP
        /// </summary>
        /// <param name="data">datos a comprimir de tipo listado del tipo diccionario.</param>
        /// <returns>Retorna string.</returns>
        public static string CompressTo(List<IDictionary<string, object>> data)
        {
            var json = JsonSerializer.Serialize(data, s_NamedFloatingPointLiterals);
            return CompressTo(json);
        }

        private static byte[] UnZip(this byte[] data) {
            using (var msi = new MemoryStream(data))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(msi, CompressionMode.Decompress))
                {
                    gs.CopyTo(mso);
                }

                return mso.ToArray();
            }
        }

        private static byte[] Zip(this byte[] data)
        {
            using (var msi = new MemoryStream(data))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(mso, CompressionMode.Compress))
                {
                    msi.CopyTo(gs);
                }

                return mso.ToArray();
            }
        }
    }

    public class ObjectNativeTypeConverter : JsonConverter<object>
    {
        public override object? Read(ref Utf8JsonReader reader, Type typeToConvert,
            JsonSerializerOptions options) => reader.TokenType switch
            {
                JsonTokenType.True => true,
                JsonTokenType.False => false,
                JsonTokenType.Number when reader.TryGetInt64(out long l) => l,
                JsonTokenType.Number => reader.GetDouble(),
                JsonTokenType.String => reader.GetString()!,
                _ => JsonDocument.ParseValue(ref reader).RootElement.Clone()
            };

        public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }
    
}
