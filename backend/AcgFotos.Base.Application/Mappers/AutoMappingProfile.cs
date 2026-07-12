using AutoMapper;
using AcgFotos.Base.Application.Dtos;
using AcgFotos.Base.Domain.Entities;
using AcgFotos.Base.Infrastructure.Repositories.Projections;

namespace AcgFotos.Base.Application.Mappers
{

    public class AutoMappingProfile : Profile
    {
        public AutoMappingProfile()
        {
            // Agregados migrados 100% a Mapperly (sin red AutoMapper): Tenant, Menu, Rol,
            // TipoLicencia, Aplicacion, Permiso, Usuario y Parametro. Lo que queda acá son los
            // agregados todavía NO migrados: Endpoint, LogInfo, Auditoria, UsuarioHistorial,
            // UsuariosActivosMensual, UsuarioTipoLicencia (licencias), TenantFile (files) y
            // ParametroValorTenant.
            this.CreateMap<EndpointDto, Endpoint>().ReverseMap();
            // LoggerError Serilog
            this.CreateMap<LogInfo, LogInfoDto>().ReverseMap();

            this.CreateMap<AuditoriaDto, Auditoria>().ReverseMap()
                .ForMember(dest => dest.UsuarioNombre, opt => opt.MapFrom(src => src.Usuario.UserName));

            // UsuarioHistorial ya no tiene navegación a Usuario: Nombre/UserName/Email
            // se resuelven por LEFT JOIN en UsuarioHistorialReportProjection (reportes).
            this.CreateMap<UsuarioHistorialDto, UsuarioHistorial>().ReverseMap();
            this.CreateMap<UsuarioHistorialReportProjection, UsuarioHistorialDto>();

            this.CreateMap<UsuariosActivosMensualDto, UsuariosActivosMensual>().ReverseMap();

            this.CreateMap<UsuarioTipoLicenciaDto, UsuarioTipoLicencia>().ReverseMap();

            // TenantFile → DTO (read). Url la puebla el FileAppService (no se mapea acá).
            this.CreateMap<TenantFile, TenantFileDto>()
                .ForMember(dest => dest.Url, opt => opt.Ignore());

            this.CreateMap<ParametroValorTenant, ParametroValorTenantDto>()
                .ReverseMap();
        }
    }
}
