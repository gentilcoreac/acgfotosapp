using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using AutoMapper;
using ClosedXML.Excel;
using Microsoft.Extensions.Configuration;
using AcgFotos.Base.Application.Dtos;
using AcgFotos.Base.Application.IServices;
using AcgFotos.Base.Domain.Entities;
using AcgFotos.Base.Infrastructure.Repositories;
using AcgFotos.Core.Application;
using AcgFotos.Core.Data;
using AcgFotos.Core.Email;
using AcgFotos.Core.Exceptions;
using AcgFotos.Core.ExtensionMethods;
using AcgFotos.Core.Session;

namespace AcgFotos.Base.Application.Services
{
    public class UsuariosActivosMensualAppService : EntityAppServiceBase<UsuariosActivosMensual,
                                                                         UsuariosActivosMensualDto,
                                                                         ListaPaginadaCriteriaBase>, IUsuariosActivosMensualAppService
    {
        private readonly IUsuariosActivosMensualRepository _usuariosActivosMensualRepository;
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IConfiguration _configuration;
        private readonly IEmailSender _emailSender;

        public UsuariosActivosMensualAppService(
            IUnitOfWork unitOfWork,
            IEntityBaseRepository<UsuariosActivosMensual> entityRepository,
            IUsuariosActivosMensualRepository usuariosActivosMensualRepository,
            IUsuarioRepository usuarioRepository,
            IEmailSender emailSender,
            IConfiguration configuration,
            IAppContext appContext,
            IMapper mapper) : base(unitOfWork, entityRepository, appContext, mapper)
        {
            _usuariosActivosMensualRepository = usuariosActivosMensualRepository;
            _usuarioRepository = usuarioRepository;
            _configuration = configuration;
            _emailSender = emailSender;
        }

        public override async Task<PaginationSet<UsuariosActivosMensualDto>> SearchAsync(ListaPaginadaCriteriaBase criteria)
        {
            var page = await _usuariosActivosMensualRepository.PaginateByTenantWithSearchAsync(
                criteria, this.AppContext.TenantId);

            var dtos = this.Mapper
                .Map<IEnumerable<UsuariosActivosMensual>, IEnumerable<UsuariosActivosMensualDto>>(page.Items)
                .ToList();

            if (dtos.Count > 0)
            {
                // Cargamos el historial de todos los Periodos de la página en una sola query
                // (evita N+1) y lo agrupamos en memoria por Periodo.
                var periodos = dtos.Select(d => d.Periodo).Distinct().ToList();
                var historialEntities = await _usuarioRepository.GetUsuarioHistorialByPeriodosAndTenantAsync(
                    periodos, this.AppContext.TenantId);
                var historialPorPeriodo = historialEntities
                    .GroupBy(h => h.Periodo)
                    .ToDictionary(g => g.Key, g => g.ToList());

                foreach (var dto in dtos)
                {
                    if (historialPorPeriodo.TryGetValue(dto.Periodo, out var historial))
                    {
                        dto.UsuariosHistorial = this.Mapper.Map<List<UsuarioHistorialDto>>(historial);
                    }
                }
            }

            return page.WithItems(dtos);
        }

        public async Task<UsuariosActivosMensualDto> CalcularLicenciasActivasAsync()
        {
            var firstDay = new DateTime(DateTime.Today.AddMonths(-1).Year,
                                        DateTime.Today.AddMonths(-1).Month, 1);

            var currentCulture = CultureInfo.GetCultureInfo("es-AR");
            var ultimoPeriodo = firstDay.ToString("yyyy MMMM", currentCulture);

            var reportePeriodoExistente = await _usuariosActivosMensualRepository
                .GetByPeriodoAndTenantIdAsync(ultimoPeriodo, this.AppContext.TenantId);

            var historial = await _usuarioRepository.GetUsuarioHistorialByPeriodoAndTenantAsync(
                ultimoPeriodo, this.AppContext.TenantId);

            var usuariosHistorialDto = historial.Select(x => this.Mapper.Map<UsuarioHistorialDto>(x)).ToList();

            UsuariosActivosMensualDto usuariosActivosMensuales;
            if (reportePeriodoExistente != null)
            {
                usuariosActivosMensuales = this.Mapper.Map<UsuariosActivosMensualDto>(reportePeriodoExistente);
                usuariosActivosMensuales.UsuariosHistorial = usuariosHistorialDto;
                usuariosActivosMensuales.Cantidad = usuariosHistorialDto.Count;
            }
            else
            {
                usuariosActivosMensuales = new UsuariosActivosMensualDto
                {
                    Periodo = ultimoPeriodo,
                    Cantidad = usuariosHistorialDto.Count,
                    UsuariosHistorial = usuariosHistorialDto
                };
            }

            await this.UpdateAsync(usuariosActivosMensuales);
            return usuariosActivosMensuales;
        }

        public async Task ExportarReportePorPeriodoAsync(int licenciasActivasID)
        {
            var emailSection = _configuration.GetSection("Email");
            var email = emailSection.GetValue<string>("ReporteUsuariosActivosMensual");

            byte[] arrBytes;
            string asunto;

            using (var scope = TransactionScopeFactory.CreateReadCommitted())
            {
                var usuariosActivosMensuales = await this.EntityRepository.GetByIdAsync(licenciasActivasID);
                if (usuariosActivosMensuales == null)
                {
                    throw new BusinessException($"Registro de licencias activas Id {licenciasActivasID} no encontrado.");
                }

                var historial = await _usuarioRepository.GetUsuarioHistorialByPeriodoAndTenantAsync(
                    usuariosActivosMensuales.Periodo, this.AppContext.TenantId);

                asunto = $"REPORTE DE LICENCIAS ACTIVAS - {usuariosActivosMensuales.Periodo}";

                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Licencias");

                worksheet.Cell(1, 1).Value = "NOMBRE DE USUARIO";
                worksheet.Cell(1, 2).Value = "APELLIDO Y NOMBRE";
                worksheet.Cell(1, 3).Value = "EMAIL";
                worksheet.Range(1, 1, 1, 3).Style.Font.Bold = true;

                var rows = historial.ToList();
                for (int i = 0; i < rows.Count; i++)
                {
                    var r = rows[i];
                    worksheet.Cell(i + 2, 1).Value = r.UserName;
                    worksheet.Cell(i + 2, 2).Value = r.Nombre + " " + r.Apellido;
                    worksheet.Cell(i + 2, 3).Value = r.Email;
                }

                worksheet.Columns().AdjustToContents();

                using var ms = new MemoryStream();
                workbook.SaveAs(ms);
                arrBytes = ms.ToArray();

                scope.Complete();
            }

            using var excelFile = new MemoryStream(arrBytes);
            await _emailSender.SendAsync(asunto, "", email, excelFile, asunto + ".xlsx");
        }
    }
}
