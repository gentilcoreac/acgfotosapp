using System.Threading.Tasks;
using AcgFotos.Core.Application;
using AcgFotos.Base.Application.Dtos;
using AcgFotos.Base.Domain.Entities;

namespace AcgFotos.Base.Application.IServices
{
    public interface IUsuariosActivosMensualAppService : IEntityAppServiceBase<UsuariosActivosMensual,
                                                                                UsuariosActivosMensualDto,
                                                                                ListaPaginadaCriteriaBase>
    {
        Task<UsuariosActivosMensualDto> CalcularLicenciasActivasAsync();

        Task ExportarReportePorPeriodoAsync(int licenciasActivasID);
    }
}
