using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Npgsql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using AcgFotos.Base.Application.Dtos;
using AcgFotos.Base.Application.InputValidators;
using AcgFotos.Base.Application.IServices;
using AcgFotos.Base.Application.Outputs;
using AcgFotos.Base.Domain.Entities;
using AcgFotos.Base.Infrastructure.Data;
using AcgFotos.Base.Infrastructure.Repositories;
using AcgFotos.Base.Infrastructure.Repositories.Projections;
using AcgFotos.Core.Application;
using AcgFotos.Core.Data;
using AcgFotos.Core.Domain;
using AcgFotos.Core.Exceptions;
using AcgFotos.Core.Localization.APIResources;
using AcgFotos.Core.Session;

namespace AcgFotos.Base.Application.Services
{
    public class UsuarioAppService : ExtendedEntityAppServiceBase<Usuario,
                                                                  UsuarioInputDto,
                                                                  UsuarioDto,
                                                                  UsuarioHeaderDto,
                                                                  ListaPaginadaCriteriaBase>, IUsuarioAppService
    {
        private readonly ISecurityManagerService _securityManagerService;
        private readonly AcgFotosDbContext _dbContext;
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IPermisoRepository _permisoRepository;
        private readonly IRolRepository _rolRepository;
        private readonly IEntityBaseRepository<UsuarioRol> _usuarioRolRepository;
        private readonly IEntityBaseRepository<UsuarioAplicacion> _usuarioAplicacionRepository;
        private readonly UserManager<Usuario> _userManager;
        private readonly IConfiguration _configuration;
        private readonly ILicensesAppService _licensesAppService;
        private readonly Mappers.Mapperly.UsuarioMapper _usuarioMapper;
        private readonly Mappers.Mapperly.PermisoMapper _permisoMapper;

        public UsuarioAppService(
            IUnitOfWork unitOfWork,
            IEntityBaseRepository<Usuario> entityRepository,
            IUsuarioRepository usuarioRepository,
            IPermisoRepository permisoRepository,
            IRolRepository rolRepository,
            IEntityBaseRepository<UsuarioRol> usuarioRolRepository,
            IEntityBaseRepository<UsuarioAplicacion> usuarioAplicacionRepository,
            UserManager<Usuario> userManager,
            AcgFotosDbContext dbContext,
            IConfiguration configuration,
            IAppContext appContext,
            IMapper mapper,
            ISecurityManagerService securityManagerService,
            ILicensesAppService licensesAppService,
            Mappers.Mapperly.UsuarioMapper usuarioMapper,
            Mappers.Mapperly.PermisoMapper permisoMapper) : base(unitOfWork, entityRepository, appContext, mapper)
        {
            _securityManagerService = securityManagerService;
            _dbContext = dbContext;
            _usuarioRepository = usuarioRepository;
            _permisoRepository = permisoRepository;
            _rolRepository = rolRepository;
            _usuarioRolRepository = usuarioRolRepository;
            _usuarioAplicacionRepository = usuarioAplicacionRepository;
            _userManager = userManager;
            _configuration = configuration;
            _licensesAppService = licensesAppService;
            _usuarioMapper = usuarioMapper;
            _permisoMapper = permisoMapper;
        }

        #region Add | Usuarios - Roles

        private async Task AddUsuarioRolDefaultAsync(long tenantId, UsuarioDto usuarioDto)
        {
            var roles = await _rolRepository.GetDefaultParaNuevoTenantAsync();
            foreach (var rol in roles)
            {
                var usuarioRol = new UsuarioRol
                {
                    RolId = rol.Id,
                    UsuarioId = usuarioDto.Id,
                    TenantId = tenantId
                };
                _usuarioRolRepository.Add(usuarioRol);
                await this.UnitOfWork.CommitAsync();
            }
        }

        #endregion

        #region Add | Usuario - Aplicaciones

        private async Task AddUsuarioAplicacionesDefaultAsync(long tenantId, UsuarioDto usuarioDto)
        {
            var aplicaciones = await _dbContext.Aplicaciones
                .Where(x => x.TenantAplicaciones.Any(y => y.TenantId == tenantId))
                .ToListAsync();
            foreach (var aplicacion in aplicaciones)
            {
                var usuarioAplicacion = new UsuarioAplicacion
                {
                    AplicacionId = aplicacion.Id,
                    UsuarioId = usuarioDto.Id,
                    TenantId = tenantId,
                    Default = aplicacion.Id == 1
                };
                _usuarioAplicacionRepository.Add(usuarioAplicacion);
                await this.UnitOfWork.CommitAsync();
            }
        }

        #endregion

        #region UPDATEs

        /// <summary>
        /// Update público: recibe UsuarioInputDto (shape defensivo), carga la entidad una sola vez
        /// (tracked, con Roles + UsuarioAplicaciones para el flujo de edición), arma el UsuarioDto
        /// completo preservando campos sensibles desde la entidad, y delega al pipeline interno.
        /// La entidad tracked se reutiliza en todo el flujo — sin doble fetch.
        /// </summary>
        public override async Task<UsuarioDto> UpdateAsync(UsuarioInputDto inputDto)
        {
            var isNew = (inputDto.Id == 0);
            if (isNew)
            {
                var newDto = BuildDtoFromInput(inputDto);
                return await this.UpdateInternalAsync(newDto, currentEntity: null);
            }

            var currentEntity = await this.GetEntityToUpdateAsync(inputDto.Id)
                ?? throw new BusinessValidationException(MessagesAPI.ErrorUserNotFound);
            var dto = this.ResolveDtoPreservingSensitiveFields(inputDto, currentEntity);
            return await this.UpdateInternalAsync(dto, currentEntity);
        }

        /// <summary>
        /// Arma el UsuarioDto completo desde la entidad existente y luego pisa con los campos
        /// editables del InputDto. Ancla del fix de mass assignment: los campos que el InputDto
        /// NO expone (Administrador, EmailConfirmed, Identity stamps, TenantId) quedan intactos
        /// porque <see cref="ApplyInputToDto"/> sólo asigna los editables (no los toca).
        /// </summary>
        private UsuarioDto ResolveDtoPreservingSensitiveFields(UsuarioInputDto input, Usuario currentEntity)
        {
            var dto = _usuarioMapper.ToDto(currentEntity);
            ApplyInputToDto(input, dto);
            return dto;
        }

        /// <summary>
        /// Alta: arma el UsuarioDto de trabajo desde el InputDto. Los campos sensibles que el InputDto
        /// no expone (TenantId, Administrador, EmailConfirmed, SecurityStamp, UltimoLogin, PhoneNumber)
        /// quedan en default — TenantId se fija luego en el pipeline; los stamps los genera Identity.
        /// Las colecciones se asignan por referencia (mismo tipo de DTO en input y output).
        /// </summary>
        private static UsuarioDto BuildDtoFromInput(UsuarioInputDto input) => new UsuarioDto
        {
            Id = input.Id,
            UserName = input.UserName,
            Nombre = input.Nombre,
            Apellido = input.Apellido,
            Email = input.Email,
            Telefono = input.Telefono,
            ProfilePicture = input.ProfilePicture,
            Bloqueado = input.Bloqueado,
            Roles = input.Roles,
            UsuarioAplicaciones = input.UsuarioAplicaciones,
            TipoLicenciaActiva = input.TipoLicenciaActiva,
        };

        /// <summary>
        /// Edición: pisa sobre el UsuarioDto (ya cargado desde la entidad con los sensibles) sólo los
        /// campos editables del InputDto. Defensa de mass assignment: Administrador, EmailConfirmed,
        /// Identity stamps, TenantId y UltimoLogin NO se tocan → se preservan desde la entidad.
        /// Reemplaza al mapeo AutoMapper con .Ignore() por asignación explícita (más auditable).
        /// </summary>
        private static void ApplyInputToDto(UsuarioInputDto input, UsuarioDto dto)
        {
            dto.UserName = input.UserName;
            dto.Nombre = input.Nombre;
            dto.Apellido = input.Apellido;
            dto.Email = input.Email;
            dto.Telefono = input.Telefono;
            dto.ProfilePicture = input.ProfilePicture;
            dto.Bloqueado = input.Bloqueado;
            dto.Roles = input.Roles;
            dto.UsuarioAplicaciones = input.UsuarioAplicaciones;
            dto.TipoLicenciaActiva = input.TipoLicenciaActiva;
        }

        /// <summary>
        /// Alta: construye la entidad Usuario desde el UsuarioDto (write path ADR-0001, sin mapper).
        /// Sólo se asignan los campos editables; el TenantId lo fija el caller y los campos de Identity
        /// (PasswordHash, SecurityStamp, normalized, etc.) los genera SecurityManager.Register.
        /// </summary>
        private static Usuario BuildUsuarioEntity(UsuarioDto dto) => new Usuario
        {
            UserName = dto.UserName,
            Nombre = dto.Nombre,
            Apellido = dto.Apellido,
            Email = dto.Email,
            Telefono = dto.Telefono,
            ProfilePicture = dto.ProfilePicture,
            // Fecha de alta: antes quedaba en default (0001-01-01) porque nadie la seteaba; alimenta
            // el reporte de altas por mes (homepage). En el Update se preserva por no tocarse.
            DateCreated = System.DateTime.Now,
            // Bloqueo vía LockoutEnd (ADR-0006). LockoutEnabled lo pone Identity en CreateAsync
            // (AllowedForNewUsers=true) → el user nace "blockeable" pero NO bloqueado (LockoutEnd null).
            LockoutEnd = dto.Bloqueado ? System.DateTimeOffset.MaxValue : (System.DateTimeOffset?)null,
            Administrador = dto.Administrador,
            EmailConfirmed = dto.EmailConfirmed,
            Roles = dto.Roles?.Select(r => new UsuarioRol { RolId = r.RolId, UsuarioId = r.UsuarioId }).ToList()
                    ?? new List<UsuarioRol>(),
            UsuarioAplicaciones = dto.UsuarioAplicaciones?.Select(a => new UsuarioAplicacion
            {
                AplicacionId = a.AplicacionId,
                UsuarioId = a.UsuarioId,
                Default = a.Default
            }).ToList() ?? new List<UsuarioAplicacion>(),
        };

        /// <summary>
        /// Lógica de negocio del Update (licencias + email check + admin tenant). Trabaja con
        /// UsuarioDto completo (shape interno post-resolve) y reutiliza la currentEntity tracked
        /// cargada en la entrada pública. En create, currentEntity es null y el flujo va por
        /// SecurityManager.Register.
        /// </summary>
        private async Task<UsuarioDto> UpdateInternalAsync(UsuarioDto dto, Usuario currentEntity)
        {
            try
            {
                if (this.EmailExists(dto.Email, dto.Id))
                {
                    throw new BusinessValidationException(MessagesAPI.ErrorEmailExists);
                }

                UsuarioDto usuarioDto;

                if (dto.TipoLicenciaActiva == null || dto.TipoLicenciaActiva.TipoLicenciaId == 0)
                {
                    usuarioDto = await this.UpdateAsync(this.AppContext.TenantId, dto, currentEntity);
                }
                else
                {
                    var disponibles = await _licensesAppService.GetCantidadLicenciasDisponiblesAsync();
                    var licenciasDisponiblePorTipo = disponibles
                        .FirstOrDefault(x => x.TipoLicenciaId == dto.TipoLicenciaActiva.TipoLicenciaId);
                    var existeCambioAsignacionLicencia = dto.TipoLicenciaActiva.Id == 0;
                    var cantidadLicenciasInsuficientes = licenciasDisponiblePorTipo.CantidadDisponible == 0;

                    if (existeCambioAsignacionLicencia)
                    {
                        if (licenciasDisponiblePorTipo.IsExpired)
                        {
                            throw new BusinessValidationException(MessagesAPI.ErrorLicenseExpired);
                        }
                        if (cantidadLicenciasInsuficientes)
                        {
                            throw new BusinessValidationException(MessagesAPI.ErrorLicensesAmount);
                        }
                    }

                    if (dto.Administrador && !dto.TipoLicenciaActiva.IsActive)
                    {
                        throw new BusinessValidationException(MessagesAPI.ErrorAdminLicensesNotAssaigned);
                    }

                    usuarioDto = await this.UpdateAsync(this.AppContext.TenantId, dto, currentEntity);

                    if (dto.Id != 0)
                    {
                        var licenciaActiva = await _licensesAppService.GetLicenciaActivaAsync(currentEntity.Id);

                        if (licenciaActiva != null)
                        {
                            if (licenciaActiva.TipoLicenciaId != dto.TipoLicenciaActiva.TipoLicenciaId ||
                                (licenciaActiva.TipoLicenciaId == dto.TipoLicenciaActiva.TipoLicenciaId &&
                                 licenciaActiva.IsActive != dto.TipoLicenciaActiva.IsActive))
                            {
                                await _licensesAppService.DeactivateUserLicenseAsync(licenciaActiva.Id);

                                if (!dto.TipoLicenciaActiva.IsActive)
                                {
                                    // Stamp directo (no _userManager.UpdateSecurityStampAsync): su UserValidator
                                    // re-trackea el Usuario ya cargado por el repo => conflicto de tracking (500).
                                    currentEntity.SecurityStamp = Guid.NewGuid().ToString();
                                }
                            }
                        }
                        await _licensesAppService.AssignOrUpdateUserLicenseAsync(dto.TipoLicenciaActiva, AppContext.TenantId);
                    }
                    else
                    {
                        dto.TipoLicenciaActiva.UsuarioId = usuarioDto.Id;
                        await _licensesAppService.AssignOrUpdateUserLicenseAsync(dto.TipoLicenciaActiva, AppContext.TenantId);
                    }
                }

                // Alta nueva sin aplicaciones asignadas por el form → asignar las del tenant por
                // default. Garantiza que cualquier usuario creado desde la UI pueda navegar el menú
                // sin configuración manual post-alta (el AplicacionId es obligatorio para los menús).
                if (dto.Id == 0 && (usuarioDto.UsuarioAplicaciones == null || !usuarioDto.UsuarioAplicaciones.Any()))
                {
                    await this.AddUsuarioAplicacionesDefaultAsync(this.AppContext.TenantId, usuarioDto);
                }

                await this.UnitOfWork.CommitAsync();
                return usuarioDto;
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null &&
                    ex.InnerException.GetType() == typeof(BusinessValidationException))
                {
                    throw ex.InnerException;
                }
                throw;
            }
        }

        public async Task<UsuarioDto> CreateTenantAdminUserAsync(long tenantId, UsuarioDto dto)
        {
            try
            {
                if (this.EmailExists(dto.Email, dto.Id))
                {
                    throw new BusinessValidationException(MessagesAPI.ErrorEmailExists);
                }

                // Create puro: currentEntity es null (dto.Id == 0 — Register crea desde cero).
                var adminUserCreado = await this.UpdateAsync(tenantId, dto, currentEntity: null);
                await _licensesAppService.AddDefaultLicensesToUserAsync(tenantId, adminUserCreado);
                await this.AddUsuarioRolDefaultAsync(tenantId, adminUserCreado);
                await this.AddUsuarioAplicacionesDefaultAsync(tenantId, adminUserCreado);
                return adminUserCreado;
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null &&
                    ex.InnerException.GetType() == typeof(BusinessValidationException))
                {
                    throw ex.InnerException;
                }
                throw;
            }
        }

        /// <summary>
        /// Llamado desde administración de usuarios (tenantId = AppContext.TenantId) o desde la
        /// creación de tenant (tenantId = el nuevo tenant — solo permitido para usuarios root).
        /// `currentEntity` viene cargado tracked desde la entrada pública (Update) o es null (Create).
        /// </summary>
        private async Task<UsuarioDto> UpdateAsync(long tenantId, UsuarioDto dto, Usuario currentEntity)
        {
            using (var scope = TransactionScopeFactory.CreateReadCommitted())
            {
                var userValidator = new UsuarioDtoValidator();
                var validationResult = userValidator.Validate(dto);
                if (!validationResult.IsValid)
                {
                    throw new BusinessValidationException(MessagesAPI.ErrorValidation, validationResult.Errors);
                }

                UsuarioDto result;

                if (dto.Id == 0)
                {
                    // Alta: construcción explícita de la entidad (ADR-0001: el write no pasa por
                    // un mapper; se asignan sólo los campos editables, Identity genera el resto).
                    var user = BuildUsuarioEntity(dto);

                    if (this.AppContext.TenantId != tenantId)
                    {
                        var rootTenantId = _configuration.GetValue<long>("RootTenantId");
                        if (!(this.AppContext.IsRoot && this.AppContext.TenantId == rootTenantId))
                        {
                            // No permitimos que el framework pise el valor del tenant.
                            // Este caso se da solo cuando el root está creando un tenant ID y esto desencadeno la creación del usuario.
                            // En este caso es la clase TenantAppService la que va a mandar el UsuarioDto
                            throw new BusinessValidationException(MessagesAPI.ErrorUserNoPrivilegesChangeTenantData);
                        }
                    }
                    // Confiamos en este valor de tenant porque el método es privado. Ojo si el método deja de ser privado.
                    user.TenantId = tenantId;
                    this.SetTenantToChildItems(user, 0, 10);

                    await _securityManagerService.Register(user);
                    result = _usuarioMapper.ToDto(user);
                }
                else
                {
                    result = await this.SaveUsuarioEntityAsync(dto, currentEntity);
                }

                scope.Complete();
                return result;
            }
        }

        /// <summary>
        /// Persistencia del Update sobre la entidad tracked (ADR-0001): asignación escalar explícita
        /// de los campos editables + SyncCollections por clave de negocio. Reemplaza al ExecuteEdit
        /// legacy (merge por reflexión + apareo de colecciones por índice).
        ///
        /// Defensa de mass assignment: sólo se asignan los campos editables; el resto (Identity stamps,
        /// UserName, TenantId, Administrador, DateCreated) se preserva por **no tocarse** — explícito y
        /// más claro que el blacklist del viejo ExecuteEdit. Se desvía a propósito del SetValues(entity,
        /// dto) de la base: UsuarioDto expone campos sensibles que matchean por nombre
        /// (UserName/TenantId/Administrador/SecurityStamp/EmailConfirmed) y EF los copiaría.
        ///
        /// La `currentEntity` viene tracked con Roles + UsuarioAplicaciones desde el método público.
        /// </summary>
        private async Task<UsuarioDto> SaveUsuarioEntityAsync(UsuarioDto dto, Usuario currentEntity)
        {
            currentEntity.Nombre = dto.Nombre;
            currentEntity.Apellido = dto.Apellido;
            currentEntity.Email = dto.Email;
            currentEntity.Telefono = dto.Telefono;
            // Bloqueo vía LockoutEnd (ADR-0006). No tocamos LockoutEnabled: es el gate de "se puede
            // bloquear" (root con LockoutEnabled=false queda inbloqueable a propósito).
            currentEntity.LockoutEnd = dto.Bloqueado ? System.DateTimeOffset.MaxValue : (System.DateTimeOffset?)null;
            // ProfilePicture sólo si vino una nueva (null = el front no la cambió → preservar).
            if (dto.ProfilePicture != null)
            {
                currentEntity.ProfilePicture = dto.ProfilePicture;
            }

            // Roles (UsuarioRol) por RolId. Las filas nuevas heredan UsuarioId/TenantId de la entidad.
            ChildCollectionSync.SyncBy(
                currentEntity.Roles,
                dto.Roles.Select(r => r.RolId),
                ur => ur.RolId,
                rolId => new UsuarioRol { RolId = rolId, UsuarioId = currentEntity.Id, TenantId = currentEntity.TenantId });

            // Aplicaciones (UsuarioAplicacion) por AplicacionId.
            ChildCollectionSync.SyncBy(
                currentEntity.UsuarioAplicaciones,
                dto.UsuarioAplicaciones.Select(a => a.AplicacionId),
                ua => ua.AplicacionId,
                aplicacionId => new UsuarioAplicacion { AplicacionId = aplicacionId, UsuarioId = currentEntity.Id, TenantId = currentEntity.TenantId });

            // El flag Default puede cambiar sin que cambie el set de apps → actualizarlo en las que quedan.
            foreach (var ua in currentEntity.UsuarioAplicaciones)
            {
                var match = dto.UsuarioAplicaciones.FirstOrDefault(a => a.AplicacionId == ua.AplicacionId);
                if (match != null)
                {
                    ua.Default = match.Default;
                }
            }

            await this.UnitOfWork.CommitAsync();
            return _usuarioMapper.ToDto(currentEntity);
        }

        public async Task<UsuarioDto> UpdateProfileAsync(UsuarioPerfilDto dto)
        {
            // Este metodo es self-service ("editar MI perfil")
            // y se resuelve por el contexto autenticado, NO por dto.Id. 
            // Su único uso legítimo es el del propio usuario.
            var user = await _dbContext.Usuarios
                .Include(x => x.Roles).ThenInclude(x => x.Rol)
                .Include(x => x.UsuarioAplicaciones).ThenInclude(x => x.Aplicacion)
                .FirstAsync(x => x.Id == this.AppContext.UserId);

            user.UserName = dto.UserName;
            user.Nombre = dto.Nombre;
            user.Apellido = dto.Apellido;
            user.Email = dto.Email;
            user.Telefono = dto.Telefono;

            if (dto.ProfilePicture != null)
            {
                user.ProfilePicture = this.AdjustImage(dto.ProfilePicture, 100, 100, 75);
            }

            _dbContext.Usuarios.Update(user);
            await _dbContext.SaveChangesAsync();

            return _usuarioMapper.ToDto(user);
        }

        //Agregamos TenantId de forma recursiva: a todas las entidades y listas que conforman la entidad principal 
        protected void SetTenantToChildItems(IMultiTenantEntityBase multiTenantEntityBase, int index, int limit)
        {
            var multiTenantEntityBaseType = multiTenantEntityBase.GetType();
            var piList = multiTenantEntityBaseType.GetProperties();

            foreach (var pi in piList.Where(x => x.PropertyType.Name != "Byte[]"))
            {
                var obj = pi.GetValue(multiTenantEntityBase, null);
                if (obj == null) continue;

                var implementICollection = obj.GetType().GetInterfaces()
                    .Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(ICollection<>));
                if (!implementICollection) continue;

                foreach (var item in (System.Collections.IEnumerable)obj)
                {
                    if (item is IMultiTenantEntityBase multiTenantFkSecondEntityBase)
                    {
                        multiTenantFkSecondEntityBase.TenantId = multiTenantEntityBase.TenantId;
                    }
                    index++;
                    this.SetTenantToChildItems((IMultiTenantEntityBase)item, index, limit);
                }
            }
        }


        public async Task UpdateSecurityStampToTenantUsersAsync(long tenantId)
        {
            if (!this.AppContext.IsAdmin) return;

            // Corte de sesión de los NO-admin del tenant DESACTIVADO → sus JWT vigentes fallan en
            // OnTokenValidated (ValidateSecurityStamp). Una sola sentencia (NEWID() por fila), atómica y de un
            // round-trip; al ir dentro del TransactionScope de la baja, viaja en la misma transacción. El
            // tenantId va explícito (la baja es root-only y el filtro global ocultaría al tenant objetivo, #20).
            // Los admins NO se cortan (pueden entrar a un tenant inactivo, AUTH-13).
            await _usuarioRepository.BumpSecurityStampForNonAdminsAsync(tenantId);
        }

        #endregion

        #region GETs

        protected override Task<Usuario> GetEntityToUpdateAsync(long id) =>
            _usuarioRepository.GetByIdWithRolesAndAplicacionesForUpdateAsync(id);

        public override async Task<UsuarioDto> GetByIdAsync(long id)
        {
            Usuario entity;
            UsuarioDto dto;

            if (this.AppContext.IsRoot || this.AppContext.IsAdmin)
            {
                entity = await _usuarioRepository.GetByIdWithRolesAndAplicacionesAsync(id);
                // Return null para que controller base responda 404 (en lugar de null -> 500).
                if (entity == null) return null;
                dto = _usuarioMapper.ToDto(entity);
                dto.TipoLicenciaActiva = await _licensesAppService.GetLicenciaActivaAsync(entity.Id);
                return dto;
            }

            // Usuarios no-admin solo pueden ver usuarios no-administradores.
            var idsNoAdmin = await _usuarioRepository.GetIdsNoAdministradoresAsync();
            entity = await _usuarioRepository.GetByIdWithRolesAsync(id);
            if (entity == null) return null;
            dto = _usuarioMapper.ToDto(entity);
            dto.TipoLicenciaActiva = await _licensesAppService.GetLicenciaActivaAsync(dto.Id);

            if (!idsNoAdmin.Contains(dto.Id))
            {
                throw new BusinessValidationException(MessagesAPI.ErrorUserNoPrivilegesViewUserSelected);
            }

            return dto;
        }

        public Task<UsuarioHistorial> GetUltimoLoginAsync(long userId) =>
            _usuarioRepository.GetUltimoLoginByUserIdAsync(userId);

        public async Task<UsuarioPerfilDto> GetPerfilAsync()
        {
            var usuario = await _usuarioRepository.GetByUserNameWithAplicacionesAsync(this.AppContext.UserName)
                ?? throw new BusinessValidationException(MessagesAPI.ErrorUserRecuparate);

            return new UsuarioPerfilDto
            {
                Id = usuario.Id,
                UserName = usuario.UserName,
                Nombre = usuario.Nombre,
                Apellido = usuario.Apellido,
                Email = usuario.Email,
                ProfilePicture = usuario.ProfilePicture,
                Telefono = usuario.Telefono,
                Administrador = usuario.Administrador,
                EmailConfirmed = usuario.EmailConfirmed,
                ClaveVigente = true,
            };
        }

        public async Task<List<UsuarioDatosPublicosDto>> GetUserDatosPublicosAsync()
        {
            var users = await this.EntityRepository.GetAllAsync();
            return users.Select(_usuarioMapper.ToDatosPublicosDto).ToList();
        }

        public override async Task<PaginationSet<UsuarioHeaderDto>> SearchAsync(ListaPaginadaCriteriaBase criteria)
        {
            // El front pide ordenar por "ultimoLogin" — ya está en la proyección, EF lo traduce a SQL.
            var page = await _usuarioRepository.PaginateHeadersAsync(criteria, !this.AppContext.IsAdmin);
            return page.MapItems(projection => _usuarioMapper.ToHeaderDto(projection));
        }

        public async Task<List<long>> GetUsuariosByRolIdAsync(long? rolId)
        {
            if (rolId == null)
            {
                return new List<long>();
            }

            var ids = await _usuarioRepository.GetUsuarioIdsByRolIdAsync(rolId.Value);
            return ids.ToList();
        }

        public async Task<List<UsuarioDto>> GetUsuariosPorPermisosAsync(string[] permisos)
        {
            var roles = await _permisoRepository.GetRolIdsByCodigosPermisoAsync(permisos);
            var usuarios = await _usuarioRepository.GetByRolIdsAsync(roles);
            return usuarios.Select(_usuarioMapper.ToDto).ToList();
        }

        public async Task<List<PermisoDto>> GetUsuarioPermisosAsync(string[] codigosPermiso)
        {
            var usuarioRol = await _usuarioRepository.GetByIdWithRolesAsync(this.AppContext.UserId)
                ?? throw new BusinessException(MessagesAPI.ErrorUserNotFound);

            var usuarioRolesId = usuarioRol.Roles.Select(x => x.RolId).ToList();
            var permisos = await _permisoRepository.GetByRolIdsAsync(usuarioRolesId, codigosPermiso);

            return permisos.Select(_permisoMapper.ToDto).ToList();
        }

        #endregion

        #region DELETEs

        public override async Task DeleteByIdAsync(long id)
        {
            try
            {
                await _securityManagerService.Delete(id);
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null &&
                    ex.InnerException.GetType() == typeof(BusinessValidationException))
                {
                    throw ex.InnerException;
                }
                throw;
            }
        }

        #endregion

        public Task ReConfirmarCuenta(long id) =>
            _securityManagerService.ReConfirmarCuenta(id);

        public Task<bool> BloquearDesbloquear(string userName, bool state) =>
            _securityManagerService.StateUser(userName, state);

        /// <summary>
        /// Endpoint dedicado para promover/demotar admin de tenant. Defensa en runtime. Controller lo valida con Permisos
        /// </summary>
        public async Task SetAdministradorAsync(long id, bool administrador)
        {
            if (!this.AppContext.IsRoot && !this.AppContext.IsAdmin)
            {
                throw new BusinessValidationException(MessagesAPI.ErrorUserNoPrivilegesChangeTenantData);
            }

            var usuario = await this.GetEntityToUpdateAsync(id)
                ?? throw new BusinessValidationException(MessagesAPI.ErrorUserNotFound);

            usuario.Administrador = administrador;
            await this.UnitOfWork.CommitAsync();
        }

        public async Task<List<RolEfectivoOutput>> GetRolesEfectivosAsync(long usuarioId)
        {
            var roles = await _usuarioRepository.GetRolesEfectivosByUserIdAsync(usuarioId);
            return roles.Select(r => new RolEfectivoOutput
            {
                RolId = r.RolId,
                RolDescripcion = r.RolDescripcion,
                Origen = r.Origen,
                GrupoId = r.GrupoId,
                GrupoNombre = r.GrupoNombre,
                PermitidoPorLicencia = r.PermitidoPorLicencia
            }).ToList();
        }

        private byte[] AdjustImage(byte[] image, int width, int height, int quality)
        {
            using var stream = new MemoryStream(image);
            using var imagen = Image.Load(stream);
            var options = new JpegEncoder { Quality = quality };
            imagen.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(width, height),
                Mode = ResizeMode.Max
            }));

            using var resultStream = new MemoryStream();
            imagen.Save(resultStream, options);
            return resultStream.ToArray();
        }

        public async Task<List<UsuariosActividadMensualOutput>> GetActividadMensualAsync(int meses)
        {
            if (meses <= 0)
            {
                meses = 12;
            }

            var hoy = DateTime.Today;
            // Primer día del mes más antiguo de la ventana (incluye el mes actual).
            var desde = new DateTime(hoy.Year, hoy.Month, 1).AddMonths(-(meses - 1));

            // El scope por tenant lo aplica el filtro global multi-tenant del DbContext.
            var activos = await _usuarioRepository.GetUsuariosActivosPorMesAsync(desde);
            var altas = await _usuarioRepository.GetAltasUsuariosPorMesAsync(desde);

            var activosPorMes = activos.ToDictionary(x => (x.Anio, x.Mes), x => x.Cantidad);
            var altasPorMes = altas.ToDictionary(x => (x.Anio, x.Mes), x => x.Cantidad);

            // Relleno explícito de los `meses` meses (con ceros) para que el gráfico no tenga huecos.
            var serie = new List<UsuariosActividadMensualOutput>(meses);
            for (var i = 0; i < meses; i++)
            {
                var mes = desde.AddMonths(i);
                var clave = (mes.Year, mes.Month);
                serie.Add(new UsuariosActividadMensualOutput
                {
                    Anio = mes.Year,
                    Mes = mes.Month,
                    Activos = activosPorMes.TryGetValue(clave, out var a) ? a : 0,
                    Altas = altasPorMes.TryGetValue(clave, out var al) ? al : 0,
                });
            }

            return serie;
        }

        // EmailExists: consulta directa con NpgsqlConnection para evitar materializar la entidad.
        // Sync intencional — no se justifica async para una consulta tan acotada.
        private bool EmailExists(string email, long id)
        {
            var connectionString = _configuration.GetConnectionString("SqlModuleConnection");
            // Suppress: esta conexión cruda NO debe enlistarse en el TransactionScope ambiente (p.ej.
            // el del alta de tenant). Si lo hiciera, sería una 2ª conexión dentro del scope —la 1ª es
            // la de EF— y Npgsql no soporta transacciones distribuidas/promotable (a diferencia de
            // SQL Server con MSDTC): sin Suppress, esto directamente tira una excepción en vez de
            // promoverse. Es una lectura de unicidad sobre datos committeados → correcta fuera de la
            // transacción.
            using var suppress = new TransactionScope(TransactionScopeOption.Suppress);
            using var connection = new NpgsqlConnection(connectionString);
            try
            {
                connection.Open();
                const string query = @"SELECT COUNT(*)
                                       FROM gen_Usuarios
                                       WHERE Email = @Email AND Id != @Id";

                using var command = new NpgsqlCommand(query, connection);
                command.Parameters.AddWithValue("@Id", id);
                command.Parameters.AddWithValue("@Email", email);
                command.CommandTimeout = _configuration.GetValue<int>("SqlCommandTimeout");

                // COUNT(*) en Postgres devuelve bigint (Int64), a diferencia de SQL Server (int).
                var count = (long)command.ExecuteScalar();
                return count > 0;
            }
            finally
            {
                connection.Close();
            }
        }
    }
}
