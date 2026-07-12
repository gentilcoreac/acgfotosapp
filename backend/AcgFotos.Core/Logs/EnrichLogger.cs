using AcgFotos.Core.Session;
using Serilog.Context;

namespace AcgFotos.Core.Logs
{
    public class EnrichLogger : IEnrichLogger
    {
        private readonly IAppContext _appContext;
        public EnrichLogger(IAppContext appContext)
        {
            _appContext = appContext;
        }

        public void EnrichProperties()
        {
            LogContext.PushProperty("TenantId", _appContext.TenantId);
        }
    }
}
