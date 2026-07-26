using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Data.Common;
using System.Runtime.CompilerServices;


namespace AcgFotos.Core.Infrastructure
{
    public class DatabaseFactory
    {
        private static readonly string s_sqlConnection = "SqlModuleConnection";
        private static readonly string s_sqlCrossCuttingConnection = "SqlCrossCuttingConnection";

        // El código base (heredado de SQL Server, donde datetime2 no distingue Kind) usa DateTime.Now
        // en decenas de lugares para columnas mapeadas a "timestamp with time zone". Npgsql 6+ rechaza
        // en runtime cualquier DateTime que no sea Kind=Utc contra ese tipo — este switch legacy
        // restaura el comportamiento tolerante anterior (guarda el valor tal cual, sin validar el Kind).
        // Correcto a mediano plazo sería auditar cada call site y pasar a DateTime.UtcNow; por ahora,
        // dado el volumen heredado, se documenta como deuda en vez de tocar todo el código de una vez
        // (ver docs/06-deploy.md).
        [ModuleInitializer]
        internal static void EnableLegacyTimestampBehavior()
        {
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        }

        public static DbContextOptionsBuilder ConfigureEFProvider(DbContextOptionsBuilder options, IConfiguration configuration, string moduleName)
        {
            // UseLowerCaseNamingConvention (EFCore.NamingConventions): Postgres pliega a minúscula
            // cualquier identificador sin comillas, mientras que EF Core por defecto genera todo el DDL
            // con el PascalCase del modelo entre comillas dobles (preservando el case exacto). Sin esta
            // convención, TODO el SQL crudo heredado del código base (TestSeed.sql, seeds de dev, y
            // decenas de queries de test) tendría que reescribirse citando cada identificador. Con la
            // convención, las tablas/columnas se crean directamente en minúscula, así que ese SQL crudo
            // (que ya las referencia sin comillas) sigue funcionando sin tocarlo.
            return options.UseNpgsql(configuration.GetConnectionString(s_sqlConnection),
                npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsAssembly(moduleName + ".SqlMigrations");
                })
                .UseLowerCaseNamingConvention()
                // EFCore.NamingConventions es un plugin de terceros: el diff de "pending model changes"
                // que EF Core corre antes de migrar compara el modelo en vivo contra el snapshot, y con
                // este plugin el resultado no siempre es estable entre el tooling de diseño (dotnet ef) y
                // runtime (falso positivo conocido, no una migración real pendiente) — se ignora este
                // warning puntual en vez de tratarlo como error.
                .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        }

        public static string GetConnectionString(IConfiguration configuration)
        {
            return configuration.GetConnectionString(s_sqlConnection);
        }

        public static DbConnection CreateCrossCuttingDbConnection(IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString(s_sqlCrossCuttingConnection);

            if (connectionString == null)
            {
                return null;
            }

            try
            {
                return new NpgsqlConnection(connectionString);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }
    }
}
