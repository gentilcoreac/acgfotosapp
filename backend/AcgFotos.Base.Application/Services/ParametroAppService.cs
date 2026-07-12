using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using AcgFotos.Base.Application.Criterias;
using AcgFotos.Base.Application.Dtos;
using AcgFotos.Base.Application.IServices;
using AcgFotos.Base.Application.Mappers.Mapperly;
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
    public class ParametroAppService : EntityAppServiceBase<Parametro,
                                                            ParametroDto,
                                                            ParametroCriteria>, IParametroAppService
    {
        private const long AppGeneralId = 1;

        private readonly IParametroRepository _parametroRepository;
        private readonly IAplicacionAppService _aplicacionAppService;
        private readonly ParametroMapper _parametroMapper;

        public ParametroAppService(
            IUnitOfWork unitOfWork,
            IEntityBaseRepository<Parametro> entityRepository,
            IParametroRepository parametroRepository,
            IAplicacionAppService aplicacionAppService,
            IAppContext appContext,
            IMapper mapper,
            ParametroMapper parametroMapper) : base(unitOfWork, entityRepository, appContext, mapper)
        {
            _parametroRepository = parametroRepository;
            _aplicacionAppService = aplicacionAppService;
            _parametroMapper = parametroMapper;
        }

        /// <summary>
        /// Devuelve los Parametros filtrados por aplicacionId. Para cada uno, si el tenant tiene un
        /// ParametroValorTenant configurado, reemplaza Valor por el del tenant y expone tanto el Id
        /// del ParametroValor (para edición) como el valor default original (para reset).
        /// </summary>
        public async Task<List<ParametroValorOutput>> ParametrosPorTenantAplicacionAsync(ParametrosTenantCriteria parametrosValorCriteria)
        {
            // Verifica que el tenant tenga la aplicación.
            var aplicacionesHabilitadas = await _aplicacionAppService.GetAplicacionesPorTenantIdAsync(parametrosValorCriteria.TenantId);
            var aplicacion = aplicacionesHabilitadas.Find(x => x.Id == parametrosValorCriteria.AplicacionId);

            if (aplicacion == null)
            {
                return new List<ParametroValorOutput>();
            }

            var parametrosValor = await _parametroRepository.GetValoresByTenantIdAsync(parametrosValorCriteria.TenantId);
            var parametrosDefault = await _parametroRepository.GetByAplicacionIdAsync(parametrosValorCriteria.AplicacionId);

            // Indexar por ParametroId — el TenantId ya viene filtrado por la query del repo.
            var valoresPorParametroId = parametrosValor.ToDictionary(pv => pv.ParametroId);

            var parametrosOutput = parametrosDefault
                .Select(_parametroMapper.ToParametroValorOutput).ToList();

            foreach (var parametro in parametrosOutput)
            {
                parametro.ParametroValorDefaultValue = parametro.Valor;

                if (valoresPorParametroId.TryGetValue(parametro.Id, out var parametroValorTenant))
                {
                    parametro.Valor = parametroValorTenant.Valor;
                    parametro.ParametroValorTenant = parametroValorTenant.Valor;
                    parametro.ParametroValorId = parametroValorTenant.Id;
                    parametro.ParametroValorTenantId = parametroValorTenant.TenantId;
                }
            }

            return parametrosOutput;
        }

        public override async Task<ParametroDto> GetByIdAsync(long id)
        {
            var entity = await _parametroRepository.GetByIdWithAplicacionAsync(id);
            return _parametroMapper.ToDto(entity);
        }

        public override async Task<PaginationSet<ParametroDto>> SearchAsync(ParametroCriteria criteria)
        {
            var page = await _parametroRepository.PaginateWithAplicacionAsync(criteria, criteria.AplicacionId);
            return page.MapItems(_parametroMapper.ToDto);
        }

        public override async Task<IEnumerable<ParametroDto>> GetAllAsync()
        {
            var entities = await _parametroRepository.GetAllWithAplicacionAsync();
            return entities.Select(_parametroMapper.ToDto);
        }

        // El Update (base) sale por Mapperly: sin red AutoMapper para Parametro.
        protected override ParametroDto ToOutput(Parametro entity) => _parametroMapper.ToDto(entity);

        public Task<string> ValorParametroPorNombreAsync(string nombre) =>
            this.ValorParametroDeAplicacionPorNombreAsync(this.AppContext.AplicacionId, nombre);

        public async Task<Dictionary<string, string>> ObtenerParametrosPorNombresAsync(List<string> nombres)
        {
            var dic = new Dictionary<string, string>();
            foreach (var nombre in nombres)
            {
                dic.Add(nombre, await this.ValorParametroPorNombreAsync(nombre));
            }
            return dic;
        }

        public async Task<string> ValorParametroDeAplicacionPorNombreAsync(long? aplicacionId, string nombre)
        {
            Parametro parametroDefault = null;

            if (aplicacionId.HasValue)
            {
                parametroDefault = await _parametroRepository.GetByNombreAndAplicacionAsync(nombre, aplicacionId.Value);
            }

            if (parametroDefault == null)
            {
                parametroDefault = await _parametroRepository.GetByNombreAndAplicacionAsync(nombre, AppGeneralId);
            }

            if (parametroDefault == null)
            {
                throw new BusinessValidationException(string.Format(MessagesAPI.ErrorParamNotExists, nombre));
            }

            return parametroDefault.Valor;
        }
    }
}
