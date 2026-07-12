using System.Collections.Generic;
using System.Threading.Tasks;
using AcgFotos.Core.Application;
using AcgFotos.Base.Application.Dtos;
using AcgFotos.Base.Domain.Entities;

namespace AcgFotos.Base.Application.IServices
{
    public interface IAplicacionAppService : IEntityAppServiceBase<Aplicacion,
                                                                   AplicacionDto,
                                                                   ListaPaginadaCriteriaBase>
    {
        Task<List<UsuarioAplicacionDto>> GetAplicacionesPermitidasAsync();

        Task<List<AplicacionDto>> GetAplicacionesPorTenantAsync();

        Task<List<AplicacionDto>> GetAplicacionesPorTenantIdAsync(long id);
    }
}
