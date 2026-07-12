using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Data.Common;


namespace AcgFotos.Core.Infrastructure
{
    public class DatabaseFactory
    {
        private static readonly string s_sqlConnection = "SqlModuleConnection";
        private static readonly string s_sqlCrossCuttingConnection = "SqlCrossCuttingConnection";

        public static DbContextOptionsBuilder ConfigureEFProvider(DbContextOptionsBuilder options, IConfiguration configuration, string moduleName)
        {
            return options.UseSqlServer(configuration.GetConnectionString(s_sqlConnection),
                sqlOptions =>
                {
                    sqlOptions.MigrationsAssembly(moduleName + ".SqlMigrations");
                });
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
                return new SqlConnection(connectionString);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }
    }
}
