using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using AcgFotos.Base.Application.Dtos;
using AcgFotos.Base.Application.Inputs;
using AcgFotos.Base.Application.IServices;
using AcgFotos.Base.Domain.Entities;
using AcgFotos.Base.Infrastructure.Repositories;
using AcgFotos.Core.Application;
using AcgFotos.Core.Data;
using AcgFotos.Core.Exceptions;
using AcgFotos.Core.Localization.APIResources;
using AcgFotos.Core.Session;

namespace AcgFotos.Base.Application.Services
{
    /// <summary>
    /// Override por-tenant de parametros. ParametroValorTenant NO es IMultiTenantEntityBase a proposito:
    /// ROOT administra los overrides de TODOS los tenants desde una pantalla root-only centralizada
    /// (elige el tenant y lo manda en el body/param, sin impersonar). Como NO hay filtro global ni guard
    /// de escritura, el aislamiento de NO-root se hace EXPLICITO aca: un no-root solo ve/escribe/borra los
    /// overrides de SU tenant (el TenantId del body se ignora); root mantiene el cross-tenant. Cierra el
    /// leak de PVT-12/13/14/15 (hallazgo #12) sin romper el flujo de root.
    /// </summary>
    public class ParametroValorTenantAppService : EntityAppServiceBase<ParametroValorTenant,
                                                                       ParametroValorTenantDto,
                                                                       ListaPaginadaCriteriaBase>, IParametroValorTenantAppService
    {
        private readonly IParametroRepository _parametroRepository;

        public ParametroValorTenantAppService(
            IUnitOfWork unitOfWork,
            IParametroRepository parametroRepository,
            IEntityBaseRepository<ParametroValorTenant> entityRepository,
            IAppContext appContext,
            IMapper mapper) : base(unitOfWork, entityRepository, appContext, mapper)
        {
            _parametroRepository = parametroRepository;
        }

        // null = root (ve/administra todos los tenants); el propio TenantId = no-root (acotado).
        private long? ScopeTenantId => this.AppContext.IsRoot ? (long?)null : this.AppContext.TenantId;

        public override async Task<PaginationSet<ParametroValorTenantDto>> SearchAsync(ListaPaginadaCriteriaBase criteria)
        {
            var page = await _parametroRepository.PaginateValoresAsync(criteria, this.ScopeTenantId);
            return new PaginationSet<ParametroValorTenantDto>
            {
                Page = page.Page,
                TotalCount = page.TotalCount,
                TotalPages = page.TotalPages,
                Items = page.Items.Select(e => this.Mapper.Map<ParametroValorTenant, ParametroValorTenantDto>(e)).ToList()
            };
        }

        public override async Task<ParametroValorTenantDto> GetByIdAsync(long id)
        {
            var dto = await base.GetByIdAsync(id);
            if (dto == null)
            {
                return null;
            }
            // No-root no puede leer el override de otro tenant -> null => el controller responde 404.
            if (!this.AppContext.IsRoot && dto.TenantId != this.AppContext.TenantId)
            {
                return null;
            }
            return dto;
        }

        public override async Task DeleteByIdAsync(long id)
        {
            // No-root: solo borra overrides de su tenant. Si el id es de otro tenant (o no existe) -> no-op
            // (idempotente, sin filtrar la existencia del ajeno).
            if (!this.AppContext.IsRoot)
            {
                var existing = await this.EntityRepository.GetByIdAsync(id);
                if (existing == null || existing.TenantId != this.AppContext.TenantId)
                {
                    return;
                }
            }
            await base.DeleteByIdAsync(id);
        }

        public override async Task<ParametroValorTenantDto> UpdateAsync(ParametroValorTenantDto dto)
        {
            if (!this.AppContext.IsRoot)
            {
                // No-root no elige tenant: siempre el propio (el TenantId del body se ignora).
                dto.TenantId = this.AppContext.TenantId;

                // Edicion de un override existente: debe pertenecer a su tenant (no robar el de otro).
                if (dto.Id != 0)
                {
                    var existing = await this.EntityRepository.GetByIdAsync(dto.Id);
                    if (existing == null || existing.TenantId != this.AppContext.TenantId)
                    {
                        throw new BusinessValidationException(MessagesAPI.ErrorUserNoPrivilegesChangeTenantData);
                    }
                }
            }
            return await base.UpdateAsync(dto);
        }

        public async Task ValorizarParametroAsync(ParametroValorValorizarInput input)
        {
            this.CheckInputValidations(input);

            var parametro = await _parametroRepository.GetByNombreAsync(input.NombreParametro)
                ?? throw new BusinessException(string.Format(MessagesAPI.ErrorParameterNotFound, input.NombreParametro));

            var parametroValorDto = new ParametroValorTenantDto
            {
                ParametroId = parametro.Id,
                Valor = input.Valor,
                TenantId = this.AppContext.TenantId
            };

            await this.UpdateAsync(parametroValorDto);
        }
    }
}
