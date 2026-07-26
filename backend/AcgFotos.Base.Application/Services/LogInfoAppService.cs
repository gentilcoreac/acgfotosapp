using System;
using System.Collections.Generic;
using System.Text;
using AutoMapper;
using Npgsql;
using Microsoft.Extensions.Configuration;
using AcgFotos.Core.Application;
using AcgFotos.Core.Data;
using AcgFotos.Core.Exceptions;
using AcgFotos.Core.Localization.APIResources;
using AcgFotos.Core.Session;
using AcgFotos.Base.Application.Dtos;
using AcgFotos.Base.Application.IServices;
using AcgFotos.Base.Application.Outputs;
using AcgFotos.Base.Domain.Entities;

namespace AcgFotos.Base.Application.Services
{
    public class LogInfoAppService : EntityAppServiceBase<LogInfo, 
                                                          LogInfoDto, 
                                                          ListaPaginadaCriteriaBase>, ILogInfoAppService
    {
        private readonly IConfiguration _configuration;

        public LogInfoAppService(
            IUnitOfWork unitOfWork,
            IEntityBaseRepository<LogInfo> entityRepository,
            IAppContext appContext,
            IMapper mapper,
            IConfiguration configuration) : base(unitOfWork, entityRepository, appContext, mapper)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Listado LIVIANO cross-tenant (root) del log del aplicativo, con filtros opcionales (mensaje
        /// vía SearchText, nivel, rango de fechas), orden por más reciente y paginación OFFSET/FETCH.
        /// </summary>
        public PaginationSet<LogInfoAllOutput> GetForAllTenants(LogInfoCriteria criteria)
        {
            var rootTenantId = _configuration.GetValue<long>("RootTenantId");
            if (!(this.AppContext.IsRoot && this.AppContext.TenantId == rootTenantId)) {
                throw new BusinessValidationException(MessagesAPI.ErrorTenantNotRoot);
            }

            // WHERE dinámico parametrizado (filtros opcionales). Se reusa para el COUNT y la página.
            var where = new StringBuilder("WHERE 1 = 1");
            if (!string.IsNullOrWhiteSpace(criteria.SearchText)) { where.Append(" AND Message LIKE @Search"); }
            if (!string.IsNullOrWhiteSpace(criteria.Level)) { where.Append(" AND Level = @Level"); }
            if (criteria.FechaDesde.HasValue) { where.Append(" AND TimeStamp >= @Desde"); }
            if (criteria.FechaHasta.HasValue) { where.Append(" AND TimeStamp <= @Hasta"); }
            if (criteria.TenantId.HasValue) { where.Append(" AND TenantId = @TenantId"); }
            var whereSql = where.ToString();

            var logInfos = new List<LogInfoAllOutput>();
            var connectionString = _configuration.GetConnectionString("SqlModuleConnection");
            int totalRecords = 0;
            int pageSize = criteria.PageSize > 0 ? criteria.PageSize : 50;
            int skip = criteria.Page * pageSize;

            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();

                using (NpgsqlCommand countCommand = new NpgsqlCommand($"SELECT COUNT(*) FROM gen_LogInfos {whereSql}", connection))
                {
                    AddFilterParams(countCommand, criteria);
                    // COUNT(*) en Postgres devuelve bigint (Int64), a diferencia de SQL Server (int).
                    totalRecords = (int)(long)countCommand.ExecuteScalar();
                }

                // Listado LIVIANO: NO se traen MessageTemplate/Exception/Properties (nvarchar(max), pueden
                // ser enormes). El registro completo se obtiene por id (GetByIdForAllTenants). Más reciente
                // primero (Id DESC) + paginación real OFFSET/FETCH (correcta con filtros).
                string query = $@"SELECT Id, Message, Level, TimeStamp, TenantId
                                  FROM gen_LogInfos
                                  {whereSql}
                                  ORDER BY Id DESC
                                  OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";

                using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
                {
                    AddFilterParams(command, criteria);
                    command.Parameters.AddWithValue("@Skip", skip);
                    command.Parameters.AddWithValue("@Take", pageSize);

                    using (NpgsqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            logInfos.Add(new LogInfoAllOutput
                            {
                                Id = (long)reader["Id"],
                                Message = reader["Message"] as string ?? "",
                                Level = reader["Level"] as string ?? "",
                                TimeStamp = reader["TimeStamp"] != DBNull.Value ? (DateTime)reader["TimeStamp"] : DateTime.Now,
                                TenantId = reader["TenantId"] != DBNull.Value ? (long)reader["TenantId"] : -999
                            });
                        }
                    }
                }
            }

            return new PaginationSet<LogInfoAllOutput>
            {
                Items = logInfos,
                Page = criteria.Page,
                TotalPages = (int)Math.Ceiling((decimal)totalRecords / pageSize),
                TotalCount = totalRecords
            };
        }

        /// <summary>Agrega los parámetros de los filtros activos (mismo set para COUNT y página).</summary>
        private static void AddFilterParams(NpgsqlCommand cmd, LogInfoCriteria criteria)
        {
            if (!string.IsNullOrWhiteSpace(criteria.SearchText)) { cmd.Parameters.AddWithValue("@Search", "%" + criteria.SearchText + "%"); }
            if (!string.IsNullOrWhiteSpace(criteria.Level)) { cmd.Parameters.AddWithValue("@Level", criteria.Level); }
            if (criteria.FechaDesde.HasValue) { cmd.Parameters.AddWithValue("@Desde", criteria.FechaDesde.Value); }
            if (criteria.FechaHasta.HasValue) { cmd.Parameters.AddWithValue("@Hasta", criteria.FechaHasta.Value); }
            if (criteria.TenantId.HasValue) { cmd.Parameters.AddWithValue("@TenantId", criteria.TenantId.Value); }
        }

        /// <summary> Detalle COMPLETO de un log por id, cross-tenant (root). Trae Exception/Properties. </summary>
        public LogInfoAllOutput GetByIdForAllTenants(long id)
        {
            var rootTenantId = _configuration.GetValue<long>("RootTenantId");
            if (!(this.AppContext.IsRoot && this.AppContext.TenantId == rootTenantId)) {
                throw new BusinessValidationException(MessagesAPI.ErrorTenantNotRoot);
            }

            var connectionString = _configuration.GetConnectionString("SqlModuleConnection");

            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                string query = @"SELECT Id, Message, MessageTemplate, Level, TimeStamp, Exception, Properties, TenantId
                                 FROM gen_LogInfos
                                 WHERE Id = @Id";

                using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", id);

                    using (NpgsqlDataReader reader = command.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            return null;
                        }
                        return new LogInfoAllOutput
                        {
                            Id = (long)reader["Id"],
                            Message = reader["Message"] as string ?? "",
                            MessageTemplate = reader["MessageTemplate"] as string ?? "",
                            Level = reader["Level"] as string ?? "",
                            TimeStamp = reader["TimeStamp"] != DBNull.Value ? (DateTime)reader["TimeStamp"] : DateTime.Now,
                            Exception = reader["Exception"] as string ?? "",
                            Properties = reader["Properties"] as string ?? "",
                            TenantId = reader["TenantId"] != DBNull.Value ? (long)reader["TenantId"] : -999
                        };
                    }
                }
            }
        }
    }
}
