using System.Threading.Tasks;
using AcgFotos.Core.Application;
using AcgFotos.Base.Application.Dtos;
using AcgFotos.Base.Domain.Entities;

namespace AcgFotos.Base.Application.IServices
{
    public interface IUsuarioHistorialAppService : IEntityAppServiceBase<UsuarioHistorial,
                                                                         UsuarioHistorialDto,
                                                                         ListaPaginadaCriteriaBase>
    {
        Task UpdateUsuarioHistoriaAsync(long userId, long tenantId);
    }
}
