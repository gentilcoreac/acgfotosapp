using System.Collections.Generic;
using System.Threading.Tasks;
using AcgFotos.Core.Application;
using AcgFotos.Base.Application.Criterias;
using AcgFotos.Base.Application.Dtos;
using AcgFotos.Base.Application.Outputs;
using AcgFotos.Base.Domain.Entities;

namespace AcgFotos.Base.Application.IServices
{
    public interface IParametroAppService : IEntityAppServiceBase<Parametro,
                                                                  ParametroDto,
                                                                  ParametroCriteria>
    {
        Task<List<ParametroValorOutput>> ParametrosPorTenantAplicacionAsync(ParametrosTenantCriteria parametrosValorCriteria);

        Task<string> ValorParametroPorNombreAsync(string nombre);

        Task<string> ValorParametroDeAplicacionPorNombreAsync(long? aplicacionId, string nombre);

        Task<Dictionary<string, string>> ObtenerParametrosPorNombresAsync(List<string> nombres);
    }
}
