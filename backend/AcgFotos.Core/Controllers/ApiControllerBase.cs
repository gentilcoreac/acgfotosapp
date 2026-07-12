using Microsoft.AspNetCore.Mvc;
using AcgFotos.Core.AuditLog;
using AcgFotos.Core.Exceptions;
using AcgFotos.Core.Logs;
using AcgFotos.Core.Security;
using AcgFotos.Core.Session;

namespace AcgFotos.Core.Controllers
{
    [ServiceFilter(typeof(AuditLogFilterAttribute))]
    [ServiceFilter(typeof(AppContextFilterAttribute))]
    [ServiceFilter(typeof(CustomFilterExceptionAttribute))]
    [ServiceFilter(typeof(LoggerActionFilterAttribute))]
    [ServiceFilter(typeof(EndpointAuthoritation))]
    public class ApiControllerBase : ControllerBase
    {

    }
}
