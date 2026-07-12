using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Transactions;
using AutoMapper;
using AcgFotos.Base.Application.Dtos;
using AcgFotos.Base.Application.IServices;
using AcgFotos.Base.Domain.Entities;
using AcgFotos.Base.Infrastructure.Repositories;
using AcgFotos.Core.Application;
using AcgFotos.Core.Data;
using AcgFotos.Core.Session;

namespace AcgFotos.Base.Application.Services
{
    public class UsuarioHistorialAppService : EntityAppServiceBase<UsuarioHistorial,
                                                                   UsuarioHistorialDto,
                                                                   ListaPaginadaCriteriaBase>, IUsuarioHistorialAppService
    {
        private readonly IUsuarioRepository _usuarioRepository;

        public UsuarioHistorialAppService(
            IUnitOfWork unitOfWork,
            IEntityBaseRepository<UsuarioHistorial> entityRepository,
            IUsuarioRepository usuarioRepository,
            IAppContext appContext,
            IMapper mapper) : base(unitOfWork, entityRepository, appContext, mapper)
        {
            _usuarioRepository = usuarioRepository;
        }

        public async Task UpdateUsuarioHistoriaAsync(long userId, long tenantId)
        {
            var today = DateTime.Now;
            var currentCulture = CultureInfo.GetCultureInfo("es-AR");
            var periodo = today.ToString("yyyy MMMM", currentCulture);

            // Se escribe directo por el repo, NO via UpdateAsync base: el historial se registra
            // durante el login, cuando el AppContext todavía no tiene tenant (IsAnonymous) y
            // SetAppContextTenant tiraría ErrorTenantNotSelected. El TenantId viene del usuario
            // que se está logueando. El filtro multitenant no aplica en login (IsAnonymous), así
            // que el GET encuentra la fila del período sin importar el tenant.
            using (var scope = TransactionScopeFactory.CreateReadCommitted())
            {
                var historialEntity = await _usuarioRepository.GetUsuarioHistorialByPeriodoAndUserIdAsync(periodo, userId);

                if (historialEntity == null)
                {
                    historialEntity = new UsuarioHistorial
                    {
                        UsuarioId = userId,
                        TenantId = tenantId,
                        FechaLastLogin = today,
                        Periodo = periodo
                    };
                    this.EntityRepository.Add(historialEntity);
                }
                else
                {
                    historialEntity.FechaLastLogin = today;
                }

                await this.UnitOfWork.CommitAsync();
                scope.Complete();
            }
        }
    }
}
