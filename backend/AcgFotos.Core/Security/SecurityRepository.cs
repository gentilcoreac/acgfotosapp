using Microsoft.Extensions.Configuration;
using AcgFotos.Core.Infrastructure;
using AcgFotos.Core.Security;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;

namespace AcgFotos.Core.Security.Repository
{
    public class SecurityRepository : ISecurityRepository
    {

        private readonly IConfiguration _configuration;

        public SecurityRepository(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void RegisterEndpoints(List<EndpointDto> endpoints)
        {
            using (var connection = DatabaseFactory.CreateCrossCuttingDbConnection(_configuration))
            {
                DisableEndpoints(connection);
                foreach (var endpoint in endpoints)
                {
                    if (!GetEndpoint(endpoint, connection))
                    {
                        AddNewEndpoints(endpoint, connection);
                    }
                    else
                    {
                        UpdateEndpoint(endpoint, connection);
                    }
                }
            }
        }

        private static void DisableEndpoints(DbConnection connection)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("UPDATE gen_Endpoints ");
            sb.AppendLine("SET Activo = false");

            using (var command = connection.CreateCommand())
            {
                command.CommandText = sb.ToString();
                connection.Open();
                var result = command.ExecuteNonQuery();
                connection.Close();
            }
        }

        private static void UpdateEndpoint(EndpointDto endpoint, DbConnection connection)
        {
            // Identidad semántica: (Route, HttpMethod). Update para que un rename de controller
            // quede sincronizado automáticamente en el próximo discover.
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("UPDATE gen_Endpoints ");
            sb.AppendLine("SET Activo = true, ");
            sb.AppendLine("    ActionName = @ActionName, ");
            sb.AppendLine("    ControllerName = @ControllerName, ");
            sb.AppendLine("    Namespace = @Namespace, ");
            sb.AppendLine("    ModuleName = @ModuleName ");
            sb.AppendLine("WHERE Route = @Route ");
            sb.AppendLine("AND HttpMethod = @HttpMethod");

            using (var command = connection.CreateCommand())
            {
                command.CommandText = sb.ToString();

                var actionNameParam = command.CreateParameter();
                actionNameParam.ParameterName = "@ActionName";
                actionNameParam.Value = endpoint.ActionName;
                command.Parameters.Add(actionNameParam);

                var controllerNameParam = command.CreateParameter();
                controllerNameParam.ParameterName = "@ControllerName";
                controllerNameParam.Value = endpoint.ControllerName;
                command.Parameters.Add(controllerNameParam);

                var namespaceParam = command.CreateParameter();
                namespaceParam.ParameterName = "@Namespace";
                namespaceParam.Value = endpoint.Namespace;
                command.Parameters.Add(namespaceParam);

                var moduleNameParam = command.CreateParameter();
                moduleNameParam.ParameterName = "@ModuleName";
                moduleNameParam.Value = endpoint.ModuleName;
                command.Parameters.Add(moduleNameParam);

                var routeParam = command.CreateParameter();
                routeParam.ParameterName = "@Route";
                routeParam.Value = endpoint.Route;
                command.Parameters.Add(routeParam);

                var httpMethodParam = command.CreateParameter();
                httpMethodParam.ParameterName = "@HttpMethod";
                httpMethodParam.Value = endpoint.HttpMethod;
                command.Parameters.Add(httpMethodParam);

                connection.Open();
                var result = command.ExecuteNonQuery();
                connection.Close();
            }
        }


        private static bool GetEndpoint(EndpointDto endpoint, DbConnection connection)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("SELECT 1 FROM gen_Endpoints ");
            sb.AppendLine("WHERE Route = @Route ");
            sb.AppendLine("AND HttpMethod = @HttpMethod");

            using (var command = connection.CreateCommand())
            {
                command.CommandText = sb.ToString();

                var routeParam = command.CreateParameter();
                routeParam.ParameterName = "@Route";
                routeParam.Value = endpoint.Route;
                command.Parameters.Add(routeParam);

                var httpMethodParam = command.CreateParameter();
                httpMethodParam.ParameterName = "@HttpMethod";
                httpMethodParam.Value = endpoint.HttpMethod;
                command.Parameters.Add(httpMethodParam);

                connection.Open();
                var reader = command.ExecuteReader();

                while (reader.Read())
                {
                    connection.Close();
                    return true;
                }
                connection.Close();
                return false;
            }
        }

        private static void AddNewEndpoints(EndpointDto endpoint, DbConnection connection)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("INSERT INTO gen_Endpoints ");
            sb.AppendLine("(ActionName, ControllerName, Namespace, ModuleName, Route, HttpMethod, Activo) ");
            sb.AppendLine("VALUES ");
            sb.AppendLine("(@ActionName, @ControllerName, @Namespace, @ModuleName, @Route, @HttpMethod, @Activo)");

            using (var command = connection.CreateCommand())
            {
                command.CommandText = sb.ToString();

                // Añadir parámetros para evitar inyección SQL
                var actionNameParam = command.CreateParameter();
                actionNameParam.ParameterName = "@ActionName";
                actionNameParam.Value = endpoint.ActionName;
                command.Parameters.Add(actionNameParam);

                var controllerNameParam = command.CreateParameter();
                controllerNameParam.ParameterName = "@ControllerName";
                controllerNameParam.Value = endpoint.ControllerName;
                command.Parameters.Add(controllerNameParam);

                var namespaceParam = command.CreateParameter();
                namespaceParam.ParameterName = "@Namespace";
                namespaceParam.Value = endpoint.Namespace;
                command.Parameters.Add(namespaceParam);

                var moduleNameParam = command.CreateParameter();
                moduleNameParam.ParameterName = "@ModuleName";
                moduleNameParam.Value = endpoint.ModuleName;
                command.Parameters.Add(moduleNameParam);

                var routeParam = command.CreateParameter();
                routeParam.ParameterName = "@Route";
                routeParam.Value = endpoint.Route;
                command.Parameters.Add(routeParam);

                var httpMethodParam = command.CreateParameter();
                httpMethodParam.ParameterName = "@HttpMethod";
                httpMethodParam.Value = endpoint.HttpMethod;
                command.Parameters.Add(httpMethodParam);

                var activoParam = command.CreateParameter();
                activoParam.ParameterName = "@Activo";
                activoParam.Value = true; // Assuming "Activo" is always true para endpoints nuevos
                command.Parameters.Add(activoParam);

                connection.Open();
                var result = command.ExecuteNonQuery();
                connection.Close();
            }
        }

        public List<EndpointDto> GetAll(string userName, long tenantId)
        {
            using (var connection = DatabaseFactory.CreateCrossCuttingDbConnection(_configuration))
            {
                var sb = new StringBuilder();
                sb.AppendLine(" SELECT ");
                sb.AppendLine("     e.ActionName,  ");
                sb.AppendLine("     e.ControllerName,  ");
                sb.AppendLine("     e.Namespace,  ");
                sb.AppendLine("     e.ModuleName,  ");
                sb.AppendLine("     e.HttpMethod,  ");
                sb.AppendLine("     e.Route  ");
                sb.AppendLine(" FROM  gen_Endpoints e ");
                sb.AppendLine("     INNER JOIN gen_PermisoEndpoints pe ON pe.EndpointID = e.ID ");
                sb.AppendLine("     INNER JOIN gen_RolPermisos rp ON rp.PermisoID = pe.PermisoID ");
                sb.AppendLine(" WHERE e.Activo = true ");
                // Roles EFECTIVOS del usuario (directos ∪ de sus grupos): FUENTE ÚNICA DE VERDAD en la
                // vista vw_UsuarioRolesEfectivos (misma definición que consume el menú vía
                // UsuarioRepository.GetEffectiveRolIdsByUserIdAsync). El IN(...) es un semi-join: los
                // RolId repetidos (directo + por uno o más grupos) no afectan, no hace falta DISTINCT.
                // `PermitidoPorLicencia = 1`: la licencia del usuario es TOPE DURO — un rol efectivo
                // (directo o por grupo) sólo cuenta si está en su licencia activa. Root no llega acá
                // (CheckPemissions() = false para root real).
                sb.AppendLine("   AND rp.RolId IN ( ");
                sb.AppendLine("       SELECT v.RolId ");
                sb.AppendLine("       FROM vw_UsuarioRolesEfectivos v ");
                sb.AppendLine("           INNER JOIN gen_Usuarios u ON u.Id = v.UsuarioId ");
                sb.AppendLine("       WHERE u.UserName = @UserName AND u.TenantId = @TenantId ");
                sb.AppendLine("         AND v.PermitidoPorLicencia = true ");
                sb.AppendLine("   ) ");

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sb.ToString();

                    // Añadir parámetros para evitar inyección SQL
                    var userNameParam = command.CreateParameter();
                    userNameParam.ParameterName = "@UserName";
                    userNameParam.Value = userName;
                    command.Parameters.Add(userNameParam);

                    var tenantIdParam = command.CreateParameter();
                    tenantIdParam.ParameterName = "@TenantId";
                    tenantIdParam.Value = tenantId;
                    command.Parameters.Add(tenantIdParam);

                    connection.Open();
                    using (var reader = command.ExecuteReader())
                    {
                        var endpoints = new List<EndpointDto>();
                        while (reader.Read())
                        {
                            var endpoint = new EndpointDto
                            {
                                ActionName = reader.GetString(0),
                                ControllerName = reader.GetString(1),
                                Namespace = reader.GetString(2),
                                ModuleName = reader.GetString(3),
                                HttpMethod = reader.GetString(4),
                                Route = reader.GetString(5)
                            };

                            endpoints.Add(endpoint);
                        }

                        connection.Close();
                        return endpoints;
                    }
                }
            }
        }

        private string GetSecurityStamp(string userName, long tenantId)
        {
            using (var connection = DatabaseFactory.CreateCrossCuttingDbConnection(_configuration))
            {
                var sb = new StringBuilder();
                sb.AppendLine("SELECT");
                sb.AppendLine("    u.SecurityStamp");
                sb.AppendLine("FROM gen_Usuarios u");
                sb.AppendLine("WHERE u.UserName = @UserName");
                sb.AppendLine("  AND u.TenantId = @TenantId");
                sb.AppendLine("LIMIT 1");

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sb.ToString();

                    // Añadir parámetros para evitar inyección SQL
                    var userNameParam = command.CreateParameter();
                    userNameParam.ParameterName = "@UserName";
                    userNameParam.Value = userName;
                    command.Parameters.Add(userNameParam);

                    var tenantIdParam = command.CreateParameter();
                    tenantIdParam.ParameterName = "@TenantId";
                    tenantIdParam.Value = tenantId;
                    command.Parameters.Add(tenantIdParam);

                    connection.Open();
                    using (var reader = command.ExecuteReader())
                    {
                        var securityStamp = string.Empty;
                        if (reader.Read())
                        {
                            securityStamp = reader.GetString(0);
                        }

                        connection.Close();
                        return securityStamp;
                    }
                }
            }
        }

        public bool ValidateSecurityStamp(string userName, long tenantId, string securityStamp)
        {
            var currentSecurityStamp = this.GetSecurityStamp(userName, tenantId);
            return securityStamp == currentSecurityStamp;
        }
    }
}
