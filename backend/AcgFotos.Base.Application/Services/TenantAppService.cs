using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using AutoMapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using AcgFotos.Core.Application;
using AcgFotos.Core.Data;
using AcgFotos.Core.Exceptions;
using AcgFotos.Core.ExtensionMethods;
using AcgFotos.Core.Storage;
using AcgFotos.Core.Localization.APIResources;
using AcgFotos.Core.Session;
using AcgFotos.Base.Application.Dtos;
using AcgFotos.Base.Application.IServices;
using AcgFotos.Base.Application.Mappers.Mapperly;
using AcgFotos.Base.Application.Outputs;
using AcgFotos.Base.Domain.Entities;
using AcgFotos.Base.Infrastructure.Repositories;
using AcgFotos.Base.Infrastructure.Repositories.Projections;

namespace AcgFotos.Base.Application.Services
{
    public partial class TenantAppService : ExtendedEntityAppServiceBase<Tenant,
                                                                         TenantInputDto,
                                                                         TenantDto,
                                                                         TenantHeaderDto,
                                                                         ListaPaginadaCriteriaBase>, ITenantAppService
    {
        private readonly IWebHostEnvironment _env;
        private readonly ITenantRepository _tenantRepository;
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IUsuarioAppService _usuarioAppService;
        private readonly ILicensesAppService _licensesAppService;
        private readonly IStorageProvider _storageProvider;
        private readonly IConfiguration _configuration;
        private readonly TenantMapper _tenantMapper;


        public TenantAppService(
            IWebHostEnvironment env,
            IUnitOfWork unitOfWork,
            IEntityBaseRepository<Tenant> entityRepository,
            ITenantRepository tenantRepository,
            IUsuarioRepository usuarioRepository,
            IUsuarioAppService usuarioAppService,
            ILicensesAppService licensesAppService,
            IStorageProvider storageProvider,
            IAppContext appContext,
            IConfiguration configuration,
            IMapper mapper,
            TenantMapper tenantMapper) : base(unitOfWork, entityRepository, appContext, mapper)
        {
            _env = env;
            _tenantRepository = tenantRepository;
            _usuarioRepository = usuarioRepository;
            _usuarioAppService = usuarioAppService;
            _licensesAppService = licensesAppService;
            _storageProvider = storageProvider;
            _configuration = configuration;
            _tenantMapper = tenantMapper;
        }

        /// <summary>Read path por Mapperly (ADR-0001). O = TenantDto (detalle); reemplaza el AutoMapper del base.</summary>
        protected override TenantDto ToOutput(Tenant entity) => _tenantMapper.ToDto(entity);

        public async Task<List<TenantHeaderDto>> GetTenantsForUserRootAsync()
        {
            var userName = this.AppContext.UserName;
            var usuarioLogueado = await _usuarioRepository.GetByUserNameAsync(userName);

            if (usuarioLogueado == null)
            {
                throw new BusinessValidationException("ErrorUserRecuparate");
            }

            if (!this.AppContext.IsRoot)
            {
                return null;
            }

            var tenants = await _tenantRepository.GetTenantsExceptAsync(usuarioLogueado.TenantId);
            return _tenantMapper.ToHeaderDtos(tenants);
        }

        public async Task<TenantHeaderStyleDto> GetTenantStyleByIdAsync(long id)
        {
            var entity = await _tenantRepository.GetByIdReadOnlyAsync(id);
            return entity != null
                ? _tenantMapper.ToHeaderStyle(entity)
                : null;
        }

        public async Task<GetTenantPublicStyleOutput> GetTenantPublicStyleAsync(string valueToFilter)
        {
            if (string.IsNullOrEmpty(valueToFilter))
            {
                return null;
            }

            var entity = await _tenantRepository.GetByHostNameOrCodigoAsync(valueToFilter);
            return entity != null
                ? _tenantMapper.ToPublicStyle(entity)
                : null;
        }

        public override async Task<PaginationSet<TenantHeaderDto>> SearchAsync(ListaPaginadaCriteriaBase criteria)
        {
            var page = await _tenantRepository.PaginateHeadersAsync(criteria);
            return page.MapItems(_tenantMapper.ToHeaderDto);
        }

        public override async Task<TenantDto> GetByIdAsync(long id)
        {
            var entity = await _tenantRepository.GetByIdWithLicensesAndAplicacionesAsync(id);
            return entity != null ? _tenantMapper.ToDto(entity) : null;
        }

        /// <summary>
        /// Administradores del tenant (solo lectura). Endpoint dedicado, fuera de GetByIdAsync para no
        /// cargar la query cross-tenant en cada detalle. El listado nunca se reenvía en el add/edit.
        /// </summary>
        public async Task<List<TenantAdminUsuarioDto>> GetAdministradoresAsync(long tenantId)
        {
            var admins = await _usuarioRepository.GetAdministradoresByTenantIdAsync(tenantId);
            return admins
                .Select(u => new TenantAdminUsuarioDto
                {
                    Id = u.Id,
                    Nombre = u.Nombre,
                    Apellido = u.Apellido,
                    UserName = u.UserName,
                    Email = u.Email,
                })
                .ToList();
        }

        protected override Task<Tenant> GetEntityToUpdateAsync(long id) =>
            _tenantRepository.GetByIdForUpdateAsync(id);

        public override async Task<TenantDto> UpdateAsync(TenantInputDto inputDto)
        {
            var isNew = (inputDto.Id == 0);

            // Resuelve el TenantDto con shape completo a partir del Input, protegiendo
            // los campos sensibles (HasError, ErrorDescription) que el cliente no debe
            // poder setear via el body.
            var dto = await this.ResolveDtoPreservingSensitiveFieldsAsync(inputDto, isNew);

            // Override completo (no pasa por ExtendedEntityAppServiceBase.UpdateAsync) → el chequeo
            // genérico de FluentValidation no llega solo, hay que invocarlo acá explícitamente.
            // Sobre el TenantDto ya resuelto (busca TenantDtoValidator por convención, no
            // TenantInputDtoValidator, que no existe).
            this.CheckInputValidations(dto);

            var tenantExiste = await _tenantRepository.ExistsByNombreOrCodigoAsync(dto.Nombre, dto.Codigo);
            var tenantDtoIsOk = (tenantExiste && !isNew) || (!tenantExiste && isNew);

            TenantDto tenantDto;
            using (var scope = TransactionScopeFactory.CreateReadCommitted())
            {
                if (!tenantDtoIsOk)
                {
                    throw new BusinessValidationException(MessagesAPI.ErrorTenantExists);
                }

                await _licensesAppService.ValidateTenantLicensesAsync(dto);
                tenantDto = await this.SaveEntityAndFilesAsync(dto);

                if (isNew)
                {
                    dto.Usuario.Administrador = true;

                    var usuarioExistente = await _usuarioRepository.GetByUserNameAsync(dto.Usuario.UserName);
                    if (usuarioExistente != null)
                    {
                        throw new BusinessValidationException(MessagesAPI.ErrorUserExists);
                    }

                    await _usuarioAppService.CreateTenantAdminUserAsync(tenantDto.Id, dto.Usuario);
                }

                if (!tenantDto.Activo)
                {
                    // Corte de sesión de los usuarios del tenant recién desactivado (no del contexto de root).
                    // Dentro del scope → la invalidación viaja en la MISMA transacción que el Activo=false:
                    // o quedan ambos, o ninguno (sin estado inconsistente "tenant inactivo / sesiones vivas").
                    await _usuarioAppService.UpdateSecurityStampToTenantUsersAsync(tenantDto.Id);
                }

                scope.Complete();
            }

            return tenantDto;
        }

        /// <summary>
        /// Convierte un TenantInputDto en TenantDto (shape interno completo) preservando
        /// los campos sensibles que el InputDto no expone: HasError, ErrorDescription.
        /// En update, esos valores se leen de la entidad existente.
        /// En create, quedan en su default. Este merge es el ancla del fix de mass assignment.
        /// </summary>
        private async Task<TenantDto> ResolveDtoPreservingSensitiveFieldsAsync(TenantInputDto input, bool isNew)
        {
            TenantDto dto;
            if (isNew)
            {
                dto = new TenantDto();
            }
            else
            {
                var current = await _tenantRepository.GetByIdReadOnlyAsync(input.Id);
                if (current == null)
                {
                    throw new BusinessValidationException(MessagesAPI.ErrorTenantNotFound);
                }
                // ToDto trae el estado actual, incluidos HasError/ErrorDescription (campos sensibles).
                dto = _tenantMapper.ToDto(current);
            }

            // Pisa SOLO los campos editables del input. HasError/ErrorDescription no están en el
            // TenantInputDto → quedan intactos (en edición, vienen de la entidad; en alta, default).
            // Asignación explícita (sin AutoMapper): control de mass-assignment por shape del input.
            ApplyInputToDto(input, dto);
            return dto;
        }

        public async Task<TenantHeaderStyleDto> EditByAdminClientAsync(TenantEditByAdminClientInput tenantInput)
        {
            if (!this.AppContext.IsAdmin)
            {
                throw new BusinessValidationException(MessagesAPI.ErrorUserNoPrivilegesChangeTenantData);
            }

            // El admin del cliente solo autogestiona SU propio tenant (el del contexto): edit-by-admin-client
            // es self-service. Sin este chequeo, un admin podia editar el branding de OTRO tenant mandando
            // otro Id en el body (cross-tenant write). Root administra otros tenants por /tenants/update.
            if (tenantInput.Id != this.AppContext.TenantId)
            {
                throw new BusinessValidationException(MessagesAPI.ErrorUserNoPrivilegesChangeTenantData);
            }

            var current = await _tenantRepository.GetByIdWithLicensesAndAplicacionesAsync(tenantInput.Id);
            if (current == null)
            {
                throw new BusinessValidationException(MessagesAPI.ErrorTenantNotFound);
            }

            // Merge: parte del estado actual (con apps/licencias para que SyncCollections las conserve)
            // y pisa únicamente los campos que el admin del cliente puede tocar.
            var dto = _tenantMapper.ToDto(current);
            ApplyAdminClientInputToDto(tenantInput, dto);

            TenantDto tenantDto;
            using (var scope = TransactionScopeFactory.CreateReadCommitted())
            {
                tenantDto = await this.SaveEntityAndFilesAsync(dto);
                scope.Complete();
            }

            return _tenantMapper.ToHeaderStyle(tenantDto);
        }

        /// <summary>
        /// Doble save histórico del Tenant:
        ///   1) Persiste para obtener el Id (los archivos del storage se nombran con ese Id).
        ///   2) Sube los archivos a storage y resuelve URLs.
        ///   3) Re-persiste con las URLs ya seteadas en el dto.
        /// Fase E (storage abstracto) va a rediseñar este flujo a un solo save — el doble
        /// commit actual es legacy del approach de naming de archivos por TenantId.
        /// </summary>
        private async Task<TenantDto> SaveEntityAndFilesAsync(TenantDto dto)
        {
            var tenantDto = await this.SaveTenantEntityAsync(dto);
            tenantDto = await this.SaveUploadedFilesAsync(tenantDto, dto);
            tenantDto = await this.SaveTenantEntityAsync(tenantDto);
            return tenantDto;
        }

        /// <summary>
        /// Persistencia del Tenant por el patrón ADR-0001: sobre la entidad tracked, SetValues para
        /// escalares (EF-nativo; ignora navegaciones, colecciones y los miembros del dto que no son
        /// columnas — Usuario, *File, StyleSheetFile) + SyncCollections por clave de negocio. Sin
        /// AutoMapper y sin el EntityBaseRepository.Edit legacy (merge por reflexión).
        /// </summary>
        private async Task<TenantDto> SaveTenantEntityAsync(TenantDto dto)
        {
            var isNew = (dto.Id == 0);
            var entity = isNew ? new Tenant() : await this.GetEntityToUpdateAsync(dto.Id);

            if (isNew)
            {
                this.EntityRepository.Add(entity);
            }

            this.EntityRepository.SetValues(entity, dto);
            this.SyncTenantCollections(entity, dto);

            await this.UnitOfWork.CommitAsync();
            return _tenantMapper.ToDto(entity);
        }

        /// <summary>
        /// Reconcilia las colecciones del Tenant contra el dto (ADR-0001, sin reflexión):
        /// aplicaciones por AplicacionId y licencias por TipoLicenciaId. Para licencias, además
        /// del alta/baja por clave, se actualizan los escalares (cantidad/fechas) de las que quedan.
        /// </summary>
        private void SyncTenantCollections(Tenant entity, TenantDto dto)
        {
            ChildCollectionSync.SyncBy(
                entity.TenantAplicaciones,
                dto.TenantAplicaciones.Select(a => a.AplicacionId),
                ta => ta.AplicacionId,
                aplicacionId => new TenantAplicacion { AplicacionId = aplicacionId, TenantId = entity.Id });

            ChildCollectionSync.SyncBy(
                entity.TenantLicenses,
                dto.TenantLicenses.Select(l => l.TipoLicenciaId),
                tl => tl.TipoLicenciaId,
                tipoLicenciaId => new TenantLicencia { TipoLicenciaId = tipoLicenciaId, TenantId = entity.Id });

            // Escalares de las licencias que quedan (cantidad/fechas cambian sin cambiar el set).
            foreach (var lic in entity.TenantLicenses)
            {
                var match = dto.TenantLicenses.FirstOrDefault(l => l.TipoLicenciaId == lic.TipoLicenciaId);
                if (match != null)
                {
                    lic.Cantidad = (int)match.Cantidad;       // dto long → entity int (alineado al cast histórico)
                    lic.StartDatetime = match.StartDateTime;
                    lic.ExpireDatetime = match.ExpireDateTime;
                    lic.ModifiedDatetime = match.ModifiedDateTime;
                }
            }
        }

        /// <summary>
        /// Pisa en el TenantDto los campos editables del TenantInputDto (alta/edición completa).
        /// NO toca HasError/ErrorDescription (el input no los expone → se preservan).
        /// </summary>
        private static void ApplyInputToDto(TenantInputDto input, TenantDto dto)
        {
            dto.Id = input.Id;
            dto.Codigo = input.Codigo;
            dto.Nombre = input.Nombre;
            dto.TituloWeb = input.TituloWeb;
            dto.Activo = input.Activo;
            dto.HostName = input.HostName;

            dto.ColorPrimarioDark = input.ColorPrimarioDark;
            dto.ColorPrimarioLight = input.ColorPrimarioLight;
            dto.DarkModeByDefault = input.DarkModeByDefault;
            dto.LogoLoginLightUrl = input.LogoLoginLightUrl;
            dto.LogoLoginDarkUrl = input.LogoLoginDarkUrl;
            dto.LogoHeaderLightUrl = input.LogoHeaderLightUrl;
            dto.LogoHeaderDarkUrl = input.LogoHeaderDarkUrl;
            dto.FaviconUrl = input.FaviconUrl;
            dto.ImagenFondoLoginLightUrl = input.ImagenFondoLoginLightUrl;
            dto.ImagenFondoLoginDarkUrl = input.ImagenFondoLoginDarkUrl;
            dto.StyleSheetCssUrl = input.StyleSheetCssUrl;
            dto.TipoLayoutLogin = input.TipoLayoutLogin;

            dto.LogoLoginLightFile = input.LogoLoginLightFile;
            dto.LogoLoginDarkFile = input.LogoLoginDarkFile;
            dto.LogoHeaderLightFile = input.LogoHeaderLightFile;
            dto.LogoHeaderDarkFile = input.LogoHeaderDarkFile;
            dto.FaviconFile = input.FaviconFile;
            dto.ImagenFondoLoginLightFile = input.ImagenFondoLoginLightFile;
            dto.ImagenFondoLoginDarkFile = input.ImagenFondoLoginDarkFile;
            dto.StyleSheetFile = input.StyleSheetFile;

            dto.Usuario = input.Usuario;
            dto.TenantAplicaciones = input.TenantAplicaciones;
            dto.TenantLicenses = input.TenantLicenses;
        }

        /// <summary>
        /// Pisa en el TenantDto solo los campos que el admin del cliente puede editar (estilo/branding).
        /// NO toca Codigo/Nombre/HostName/HasError/Usuario/colecciones. StyleSheetFile es condicional:
        /// solo se pisa si vino uno nuevo (replica el opt.Condition que tenía el mapeo AutoMapper).
        /// </summary>
        private static void ApplyAdminClientInputToDto(TenantEditByAdminClientInput input, TenantDto dto)
        {
            dto.TituloWeb = input.TituloWeb;
            dto.Activo = input.Activo;

            dto.ColorPrimarioDark = input.ColorPrimarioDark;
            dto.ColorPrimarioLight = input.ColorPrimarioLight;
            dto.DarkModeByDefault = input.DarkModeByDefault;
            dto.LogoLoginLightUrl = input.LogoLoginLightUrl;
            dto.LogoLoginDarkUrl = input.LogoLoginDarkUrl;
            dto.LogoHeaderLightUrl = input.LogoHeaderLightUrl;
            dto.LogoHeaderDarkUrl = input.LogoHeaderDarkUrl;
            dto.FaviconUrl = input.FaviconUrl;
            dto.ImagenFondoLoginLightUrl = input.ImagenFondoLoginLightUrl;
            dto.ImagenFondoLoginDarkUrl = input.ImagenFondoLoginDarkUrl;
            dto.StyleSheetCssUrl = input.StyleSheetCssUrl;
            dto.TipoLayoutLogin = input.TipoLayoutLogin;

            dto.LogoLoginLightFile = input.LogoLoginLightFile;
            dto.LogoLoginDarkFile = input.LogoLoginDarkFile;
            dto.LogoHeaderLightFile = input.LogoHeaderLightFile;
            dto.LogoHeaderDarkFile = input.LogoHeaderDarkFile;
            dto.FaviconFile = input.FaviconFile;
            dto.ImagenFondoLoginLightFile = input.ImagenFondoLoginLightFile;
            dto.ImagenFondoLoginDarkFile = input.ImagenFondoLoginDarkFile;

            if (input.StyleSheetFile != null)
            {
                dto.StyleSheetFile = input.StyleSheetFile;
            }
        }

        public async Task<bool> IsActiveAsync(long tenantId)
        {
            var entity = await _tenantRepository.GetByIdReadOnlyAsync(tenantId);

            if (entity == null)
            {
                return false; // se podría lanzar una excepción de notfound
            }

            return entity.Activo;
        }
    }
}
