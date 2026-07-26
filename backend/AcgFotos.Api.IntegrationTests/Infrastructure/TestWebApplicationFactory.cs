using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Npgsql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Respawn;
using Respawn.Graph;
using AcgFotos.Base.Domain.Entities;
using AcgFotos.Base.Infrastructure.Data;
using AcgFotos.Core.AuditLog;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.Infrastructure
{
    /// <summary>
    /// Levanta la API en memoria (TestServer) apuntando a una base de tests DEDICADA
    /// en la misma instancia local que usa dev (localhost), SIN tocar la base de
    /// desarrollo (AcgFotos). La base se crea/migra al iniciar (idempotente).
    ///
    /// Corre en env Development para heredar la config base (JWT key vive en appsettings.Development);
    /// se sobreescriben las connection strings y se APAGA el rate limiting (el limiter global es estado
    /// in-memory compartido por el host: con cientos de tests saltarian 429 espurios; los tests de la
    /// seccion RATE lo re-habilitan con limites bajos vito <see cref="WithWebHostBuilder"/>).
    ///
    /// Entre tests se limpia con Respawn (<see cref="ResetAndSeedAsync"/>) y se re-siembra el dataset
    /// canonico (<c>TestSeed.sql</c>), de modo que cada test arranca de un estado conocido y aislado.
    /// </summary>
    public class TestWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        // DB de tests PROPIA de AcgFotos (no compartir con otras bases de tests del mismo localhost:
        // distinto historial de migraciones ⇒ compartir la base las rompe a ambas).
        public const string TestConnectionString =
            "Host=localhost;Port=5432;Database=AcgFotos_Tests;Username=postgres;Password=Root@AcgFotos2026!";

        private Respawner _respawner = null!;
        private string _seedSql = null!;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");

            // Ultima fuente de config => mayor prioridad que appsettings/user-secrets.
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    // Redirige EF (DatabaseFactory) y el ADO crudo (LogInfo/Usuario AppService) a la base de tests.
                    ["ConnectionStrings:SqlModuleConnection"] = TestConnectionString,
                    ["ConnectionStrings:SqlCrossCuttingConnection"] = TestConnectionString,
                    // Rate limiting apagado por defecto (ver nota de clase).
                    ["RateLimiting:Enabled"] = "false",
                });
            });

            // Sin hosted services en el host de tests: el AuditLogBackgroundWriter (y el
            // RefreshTokenCleanupService) corren async y sus escrituras racean/deadlockean con el reset
            // de Respawn entre tests. Los tests que necesitan el audit log drenan la cola a mano de forma
            // deterministica (DrainAuditQueue), que ademas respeta el contrato SingleReader del Channel.
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IHostedService>();
            });
        }

        /// <summary>
        /// Drena la AuditLogQueue (que sin el background writer nadie consume) y persiste el lote, de forma
        /// SINCRONA y deterministica: tras una request, el evento ya esta encolado, asi que llamar a esto
        /// lo deja en gen_AuditLogs sin depender de timing. Es el unico lector (no hay writer activo).
        /// </summary>
        public void DrainAuditQueue()
        {
            var queue = Services.GetRequiredService<AuditLogQueue>();
            using var scope = Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IAuditLogRepository>();

            var batch = new List<AuditLogModel>();
            while (queue.Reader.TryRead(out var entry))
            {
                batch.Add(entry);
            }
            if (batch.Count > 0)
            {
                repository.WriteLogs(batch);
            }
        }

        public async Task InitializeAsync()
        {
            // Acceder a Services dispara el build del host. Crea la base si no existe
            // y aplica las migraciones (esquema canonico, independiente del drift de dev).
            using (var scope = Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AcgFotosDbContext>();
                await db.Database.MigrateAsync();
            }

            _seedSql = LoadSeedSql();

            // Respawn: el overload por connection string SOLO soporta SqlDataAdapter (SQL Server) —
            // para Postgres hay que pasar un DbConnection ya abierto.
            await using (var setupConn = new NpgsqlConnection(TestConnectionString))
            {
                await setupConn.OpenAsync();
                _respawner = await Respawner.CreateAsync(setupConn, new RespawnerOptions
                {
                    DbAdapter = DbAdapter.Postgres,
                    SchemasToInclude = new[] { "public" },
                    // No tocar el historial de migraciones (sino habria que re-migrar en cada reset).
                    TablesToIgnore = new Table[]
                    {
                        "__EFMigrationsHistory",
                    },
                });
            }

            // Estado inicial conocido para el primer test.
            await ResetAndSeedAsync();
        }

        /// <summary>Limpia todas las tablas (Respawn) y re-siembra el dataset canonico. Idempotente.</summary>
        public async Task ResetAndSeedAsync()
        {
            await using var conn = new NpgsqlConnection(TestConnectionString);
            await conn.OpenAsync();

            await _respawner.ResetAsync(conn);

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = _seedSql;
            await cmd.ExecuteNonQueryAsync();

            // Vaciar la AuditLogQueue: sin background writer nadie la consume, asi que acumularia los
            // eventos auto-encolados de tests anteriores (que ademas pueden referenciar usuarios ya
            // borrados por el reset -> FK al drenar). Cada test arranca con la cola limpia.
            var queue = Services.GetRequiredService<AuditLogQueue>();
            while (queue.Reader.TryRead(out _)) { }
        }

        /// <summary>Ejecuta SQL arbitrario (arrange de un caso: mutar estado de un usuario, etc.).</summary>
        public async Task ExecuteSqlAsync(string sql)
        {
            await using var conn = new NpgsqlConnection(TestConnectionString);
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>Lee un escalar (aserciones sobre el estado de la base tras la accion).</summary>
        public async Task<T?> QueryScalarAsync<T>(string sql)
        {
            await using var conn = new NpgsqlConnection(TestConnectionString);
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            var result = await cmd.ExecuteScalarAsync();
            if (result == null || result == DBNull.Value)
            {
                return default;
            }
            // El valor ya es del tipo pedido (Guid, DateTimeOffset, byte[], enum...): cast directo, sin
            // pasar por Convert.ChangeType (que tira en los tipos que no implementan IConvertible).
            if (result is T typed)
            {
                return typed;
            }
            var target = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
            return (T)Convert.ChangeType(result, target);
        }

        /// <summary>Genera un token de confirmacion de email de Identity (mismo host → mismas keys).</summary>
        public async Task<string> GenerateEmailConfirmationTokenAsync(string userName)
        {
            using var scope = Services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
            var user = await userManager.FindByNameAsync(userName);
            return await userManager.GenerateEmailConfirmationTokenAsync(user!);
        }

        /// <summary>Genera un token de reseteo de password de Identity (para olvide/resetear-password).</summary>
        public async Task<string> GeneratePasswordResetTokenAsync(string userName)
        {
            using var scope = Services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
            var user = await userManager.FindByNameAsync(userName);
            return await userManager.GeneratePasswordResetTokenAsync(user!);
        }

        private static string LoadSeedSql()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var name = assembly.GetManifestResourceNames()
                .Single(n => n.EndsWith("TestSeed.sql", StringComparison.OrdinalIgnoreCase));
            using var stream = assembly.GetManifestResourceStream(name)!;
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        async Task IAsyncLifetime.DisposeAsync()
        {
            await base.DisposeAsync();
        }
    }
}
