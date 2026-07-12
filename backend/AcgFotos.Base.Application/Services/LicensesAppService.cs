using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.Configuration;
using AcgFotos.Base.Application.Constantes;
using AcgFotos.Base.Application.Dtos;
using AcgFotos.Base.Application.IServices;
using AcgFotos.Base.Application.Outputs;
using AcgFotos.Base.Domain.Entities;
using AcgFotos.Base.Infrastructure.Repositories;
using AcgFotos.Core.Application;
using AcgFotos.Core.Data;
using AcgFotos.Core.Exceptions;
using AcgFotos.Core.Localization.APIResources;
using AcgFotos.Core.Session;

namespace AcgFotos.Base.Application.Services
{
    public class LicensesAppService : ILicensesAppService
    {
        private readonly IUsuarioTipoLicenciaRepository _usuarioTipoLicenciaRepository;
        private readonly ITenantLicenciaRepository _tenantLicenciaRepository;
        private readonly ITipoLicenciaRepository _tipoLicenciaRepository;
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IConfiguration _configuration;
        private readonly IMapper _mapper;
        private readonly Mappers.Mapperly.UsuarioMapper _usuarioMapper;
        private readonly IAppContext _appContext;
        private readonly IUnitOfWork _unitOfWork;

        public LicensesAppService(
            IUsuarioTipoLicenciaRepository usuarioTipoLicenciaRepository,
            ITenantLicenciaRepository tenantLicenciaRepository,
            ITipoLicenciaRepository tipoLicenciaRepository,
            IUsuarioRepository usuarioRepository,
            IConfiguration configuration,
            IMapper mapper,
            Mappers.Mapperly.UsuarioMapper usuarioMapper,
            IAppContext appContext,
            IUnitOfWork unitOfWork)
        {
            _usuarioTipoLicenciaRepository = usuarioTipoLicenciaRepository;
            _tenantLicenciaRepository = tenantLicenciaRepository;
            _tipoLicenciaRepository = tipoLicenciaRepository;
            _usuarioRepository = usuarioRepository;
            _configuration = configuration;
            _mapper = mapper;
            _usuarioMapper = usuarioMapper;
            _appContext = appContext;
            _unitOfWork = unitOfWork;
        }

        public async Task<List<GetCantidadLicenciasDisponiblesOutput>> GetCantidadLicenciasDisponiblesAsync(long? tenantId = null, bool onlyActive = false)
        {
            var effectiveTenantId = tenantId ?? _appContext.TenantId;
            if (effectiveTenantId == 0)
            {
                throw new BusinessValidationException(MessagesAPI.ErrorTenantIsRoot);
            }

            var now = DateTime.Now;
            var thresholdDays = _configuration.GetValue("LicenseExpiration:ExpiringSoonThresholdDays", 30);
            var expiringSoonLimit = now.AddDays(thresholdDays);

            var tenantLicencias = await _tenantLicenciaRepository.GetByTenantWithTipoLicenciaAsync(effectiveTenantId, onlyActive);
            var asignadasPorTipo = await _usuarioTipoLicenciaRepository.GetCountActivasPorTipoByTenantAsync(effectiveTenantId);

            var resultado = new List<GetCantidadLicenciasDisponiblesOutput>();
            foreach (var tenantLicencia in tenantLicencias)
            {
                var isExpired = tenantLicencia.ExpireDatetime <= now;
                asignadasPorTipo.TryGetValue(tenantLicencia.TipoLicenciaId, out var cantidadAsignada);

                resultado.Add(new GetCantidadLicenciasDisponiblesOutput
                {
                    TipoLicenciaId = tenantLicencia.TipoLicenciaId,
                    Descripcion = tenantLicencia.TipoLicencia.Descripcion,
                    CantidadTotal = tenantLicencia.Cantidad,
                    CantidadAsignada = cantidadAsignada,
                    CantidadDisponible = tenantLicencia.Cantidad - cantidadAsignada,
                    IsExpired = isExpired,
                    IsExpiringSoon = !isExpired && tenantLicencia.ExpireDatetime <= expiringSoonLimit,
                    ExpirationDate = tenantLicencia.ExpireDatetime
                });
            }

            return resultado;
        }

        public async Task<LicenseValidationResult> ValidateUserLicenseAsync(string userName, long tenantId)
        {
            var expiration = await _usuarioTipoLicenciaRepository.GetActiveLicenseExpirationByUserNameAndTenantAsync(userName, tenantId);
            if (expiration == null)
            {
                return LicenseValidationResult.NoActiveLicense;
            }

            return expiration.Value <= DateTime.Now
                ? LicenseValidationResult.Expired
                : LicenseValidationResult.Valid;
        }

        public async Task<List<UsuarioDto>> GetUsuariosConLicenciaAsync()
        {
            var usuariosId = await _usuarioTipoLicenciaRepository.GetUsuarioIdsWithActiveLicenseByTenantAsync(_appContext.TenantId);
            var usuarios = await _usuarioRepository.GetByIdsAsync(usuariosId);
            return usuarios.Select(_usuarioMapper.ToDto).ToList();
        }

        public async Task<UsuarioTipoLicenciaDto> GetLicenciaActivaAsync(long userId)
        {
            var licenciaActiva = await _usuarioTipoLicenciaRepository.GetActiveByUserIdAsync(userId);
            return _mapper.Map<UsuarioTipoLicenciaDto>(licenciaActiva);
        }

        public async Task<IReadOnlyDictionary<long, UsuarioTipoLicenciaDto>> GetLicenciasActivasByUserIdsAsync(IReadOnlyList<long> userIds)
        {
            if (userIds.Count == 0)
            {
                return new Dictionary<long, UsuarioTipoLicenciaDto>();
            }

            var licencias = await _usuarioTipoLicenciaRepository.GetActivesByUserIdsAsync(userIds);
            return licencias.ToDictionary(
                x => x.UsuarioId,
                x => _mapper.Map<UsuarioTipoLicenciaDto>(x));
        }

        public Task<List<GetCantidadLicenciasDisponiblesOutput>> GetlistaTipoLicenciaEnUsoAsync(long? tenantId) =>
            this.GetCantidadLicenciasDisponiblesAsync(tenantId, onlyActive: true);

        public async Task DeactivateUserLicenseAsync(long licenciaId)
        {
            var current = await _usuarioTipoLicenciaRepository.GetByIdAsync(licenciaId);
            if (current == null) return;

            // Entidad tracked y scopeada al tenant (filtro multi-tenant). Asignación explícita
            // (ADR-0001): se modifica la entidad y persiste en el commit del caller.
            current.IsActive = false;
            current.TenantId = _appContext.TenantId;
        }

        public async Task AssignOrUpdateUserLicenseAsync(UsuarioTipoLicenciaDto dto, long tenantId)
        {
            if (dto.Id != 0)
            {
                // Update: sólo cambia el tipo de licencia sobre la entidad tracked (ADR-0001).
                // Persiste en el commit del caller.
                var current = await _usuarioTipoLicenciaRepository.GetByIdAsync(dto.Id);
                if (current == null) return;
                current.TipoLicenciaId = dto.TipoLicenciaId;
            }
            else
            {
                var nueva = new UsuarioTipoLicencia
                {
                    IsActive = dto.IsActive,
                    UsuarioId = dto.UsuarioId,
                    TipoLicenciaId = dto.TipoLicenciaId,
                    CreatedDatetime = DateTime.Now,
                    TenantId = tenantId,
                };
                _usuarioTipoLicenciaRepository.Add(nueva);
            }
        }

        public async Task AddDefaultLicensesToUserAsync(long tenantId, UsuarioDto usuarioDto)
        {
            var tipoLicencias = await _tipoLicenciaRepository.GetDefaultsParaNuevoTenantAsync();
            if (tipoLicencias.Count == 0) return;

            foreach (var tipoLicencia in tipoLicencias)
            {
                var nuevaLicencia = new UsuarioTipoLicenciaDto
                {
                    UsuarioId = usuarioDto.Id,
                    TipoLicenciaId = tipoLicencia.Id,
                    IsActive = true,
                };
                await this.AssignOrUpdateUserLicenseAsync(nuevaLicencia, tenantId);
            }

            await _unitOfWork.CommitAsync();
        }

        public async Task ValidateTenantLicensesAsync(TenantDto tenantDto)
        {
            // Tenant nuevo (Id == 0): todavía no existe ni tiene usuarios → no hay licencias asignadas
            // que validar. Además evita GetCantidadLicenciasDisponiblesAsync(tenantId: 0), que lanza
            // ErrorTenantIsRoot al resolver effectiveTenantId == 0. La validación de "no reducir por
            // debajo de lo asignado" solo aplica al editar un tenant existente.
            if (tenantDto.Id == 0 || tenantDto.TenantLicenses.Count == 0) return;

            var listaTipoLicenciaUso = await this.GetlistaTipoLicenciaEnUsoAsync(tenantDto.Id);
            var usoPorTipo = listaTipoLicenciaUso.ToDictionary(x => x.TipoLicenciaId);

            foreach (var dtoLicencia in tenantDto.TenantLicenses)
            {
                if (!usoPorTipo.TryGetValue(dtoLicencia.TipoLicenciaId, out var tipoLicenciaUso))
                {
                    continue;
                }

                if (tipoLicenciaUso.CantidadAsignada > dtoLicencia.Cantidad)
                {
                    var cantidadLicenciasEliminar = tipoLicenciaUso.CantidadAsignada - dtoLicencia.Cantidad;
                    throw new BusinessValidationException(string.Format(MessagesAPI.ErrorLicenseAmountAssign, cantidadLicenciasEliminar));
                }
            }
        }
    }
}
