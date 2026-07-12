using System.Collections.Generic;
using System.Threading.Tasks;
using AcgFotos.Base.Application.Constantes;
using AcgFotos.Base.Application.Dtos;
using AcgFotos.Base.Application.Outputs;

namespace AcgFotos.Base.Application.IServices
{
    public interface ILicensesAppService
    {
        Task<List<GetCantidadLicenciasDisponiblesOutput>> GetCantidadLicenciasDisponiblesAsync(long? tenantId = null, bool onlyActive = false);

        Task<LicenseValidationResult> ValidateUserLicenseAsync(string userName, long tenantId);

        Task<List<UsuarioDto>> GetUsuariosConLicenciaAsync();

        Task<UsuarioTipoLicenciaDto> GetLicenciaActivaAsync(long userId);

        /// <summary>Licencias activas para un set de usuarios. Single query (evita N+1).</summary>
        Task<IReadOnlyDictionary<long, UsuarioTipoLicenciaDto>> GetLicenciasActivasByUserIdsAsync(IReadOnlyList<long> userIds);

        Task DeactivateUserLicenseAsync(long licenciaId);

        Task AssignOrUpdateUserLicenseAsync(UsuarioTipoLicenciaDto dto, long tenantId);

        Task AddDefaultLicensesToUserAsync(long tenantId, UsuarioDto usuarioDto);

        Task ValidateTenantLicensesAsync(TenantDto dto);
    }
}
