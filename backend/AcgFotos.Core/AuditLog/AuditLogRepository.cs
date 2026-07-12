using Microsoft.Extensions.Configuration;
using AcgFotos.Core.Infrastructure;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;

namespace AcgFotos.Core.AuditLog
{
    public class AuditLogRepository : IAuditLogRepository
    {
        // Límite de SQL Server: 2100 parámetros por statement; 13 por fila → hasta 150 filas
        // por INSERT para quedar holgados.
        private const int MaxRowsPerStatement = 150;

        // Espejo de los HasMaxLength de AuditLogEFConfig: se trunca en escritura para que un valor
        // hostil o anómalo (ej. un User-Agent gigante) no haga fallar el INSERT del lote.
        private const int ServicioMaxLength = 100;
        private const int MetodoMaxLength = 100;
        private const int HttpMethodMaxLength = 10;
        private const int RequestPathMaxLength = 2000;
        private const int ClientIPMaxLength = 50;
        private const int ClientUserAgentMaxLength = 1000;
        private const int ResultStatusCodeMaxLength = 10;

        // Tope para Parametros/ResultContent (nvarchar(max)): acota el tamaño de fila — sin tope
        // un GET de lista grande dejaba filas de megabytes. Configurable por entorno.
        private const int DefaultMaxContentChars = 8000;
        private const string TruncationMarker = " ...[truncado]";

        private readonly IConfiguration _configuration;
        private readonly int _maxContentChars;

        public AuditLogRepository(IConfiguration configuration)
        {
            _configuration = configuration;
            _maxContentChars = configuration.GetValue<int?>("AuditLog:MaxContentChars") ?? DefaultMaxContentChars;
        }

        public void WriteLogs(IReadOnlyList<AuditLogModel> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                return;
            }

            using (var connection = DatabaseFactory.CreateCrossCuttingDbConnection(_configuration))
            {
                connection.Open();

                for (var offset = 0; offset < entries.Count; offset += MaxRowsPerStatement)
                {
                    var count = Math.Min(MaxRowsPerStatement, entries.Count - offset);
                    this.InsertChunk(connection, entries, offset, count);
                }
            }
        }

        private void InsertChunk(DbConnection connection, IReadOnlyList<AuditLogModel> entries, int offset, int count)
        {
            using (var command = connection.CreateCommand())
            {
                var sb = new StringBuilder();
                sb.AppendLine("INSERT INTO gen_AuditLogs");
                sb.AppendLine("(FechaHora, Duracion, Servicio, Metodo, Parametros, UsuarioId, ImpersonatedBy,");
                sb.AppendLine(" HttpMethod, RequestAbsolutePath, ClientIP, ClientUserAgent, ResultStatusCode, ResultContent)");
                sb.AppendLine("VALUES");

                for (var i = 0; i < count; i++)
                {
                    var entry = entries[offset + i];

                    sb.Append($"(@FechaHora{i}, @Duracion{i}, @Servicio{i}, @Metodo{i}, @Parametros{i}, @UsuarioId{i}, @ImpersonatedBy{i},");
                    sb.Append($" @HttpMethod{i}, @RequestAbsolutePath{i}, @ClientIP{i}, @ClientUserAgent{i}, @ResultStatusCode{i}, @ResultContent{i})");
                    sb.AppendLine(i < count - 1 ? "," : string.Empty);

                    AddParameter(command, $"@FechaHora{i}", entry.FechaHora);
                    AddParameter(command, $"@Duracion{i}", entry.Duracion);
                    AddParameter(command, $"@Servicio{i}", Truncate(entry.Servicio, ServicioMaxLength));
                    AddParameter(command, $"@Metodo{i}", Truncate(entry.Metodo, MetodoMaxLength));
                    AddParameter(command, $"@Parametros{i}", this.TruncateContent(entry.Parametros));
                    AddParameter(command, $"@UsuarioId{i}", entry.UsuarioId);
                    AddParameter(command, $"@ImpersonatedBy{i}", entry.ImpersonatedBy);
                    AddParameter(command, $"@HttpMethod{i}", Truncate(entry.HttpMethod, HttpMethodMaxLength));
                    AddParameter(command, $"@RequestAbsolutePath{i}", Truncate(entry.RequestAbsolutePath, RequestPathMaxLength));
                    AddParameter(command, $"@ClientIP{i}", Truncate(entry.ClientIP, ClientIPMaxLength));
                    AddParameter(command, $"@ClientUserAgent{i}", Truncate(entry.ClientUserAgent, ClientUserAgentMaxLength));
                    AddParameter(command, $"@ResultStatusCode{i}", Truncate(entry.ResultStatusCode, ResultStatusCodeMaxLength));
                    AddParameter(command, $"@ResultContent{i}", this.TruncateContent(entry.ResultContent));
                }

                command.CommandText = sb.ToString();
                command.ExecuteNonQuery();
            }
        }

        private static void AddParameter(DbCommand command, string name, object value)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value ?? DBNull.Value;
            command.Parameters.Add(parameter);
        }

        private static string Truncate(string value, int maxLength) =>
            value != null && value.Length > maxLength ? value.Substring(0, maxLength) : value;

        /// <summary>
        /// Tope para los campos de contenido libre: corta y deja un marcador visible para que
        /// quien diagnostica sepa que el payload original era más largo.
        /// </summary>
        private string TruncateContent(string value)
        {
            if (value == null || value.Length <= _maxContentChars)
            {
                return value;
            }

            return value.Substring(0, _maxContentChars) + TruncationMarker;
        }
    }
}
