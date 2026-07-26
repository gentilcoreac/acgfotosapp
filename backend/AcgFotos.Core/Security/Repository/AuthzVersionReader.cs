using Microsoft.Extensions.Configuration;
using AcgFotos.Core.Infrastructure;
using System.Text;

namespace AcgFotos.Core.Security.Repository
{
    /// <summary>
    /// Lee la versión global de autorización (<c>gen_AuthzVersion</c>) por SQL crudo sobre la conexión
    /// cross-cutting, mismo patrón que <see cref="SecurityRepository"/> (Core no tiene EF de Base). El
    /// SELECT es de 1 fila por PK → sub-ms y queda en el buffer de SQL Server; se lee en vivo en cada
    /// request para que un cambio de permisos se vea al instante (ADR-0003 §6.3). El incremento lo hace
    /// <c>AcgFotosDbContext.SaveChanges</c>.
    /// </summary>
    public class AuthzVersionReader : IAuthzVersion
    {
        private readonly IConfiguration _configuration;

        public AuthzVersionReader(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public long Get()
        {
            using (var connection = DatabaseFactory.CreateCrossCuttingDbConnection(_configuration))
            {
                var sb = new StringBuilder();
                sb.AppendLine("SELECT Version FROM gen_AuthzVersion ORDER BY Id LIMIT 1");

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sb.ToString();
                    connection.Open();
                    var result = command.ExecuteScalar();
                    connection.Close();
                    // Si por algún motivo la fila no existe todavía, 0 es un valor de versión válido
                    // (la clave de caché simplemente arranca en :0 hasta el primer bump).
                    return result == null || result == System.DBNull.Value ? 0L : System.Convert.ToInt64(result);
                }
            }
        }
    }
}
