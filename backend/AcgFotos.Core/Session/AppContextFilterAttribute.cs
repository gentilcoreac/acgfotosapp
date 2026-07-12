using Microsoft.AspNetCore.Mvc.Filters;

namespace AcgFotos.Core.Session
{
    public class AppContextFilterAttribute: ActionFilterAttribute
    {
        private readonly IAppContext _appContext;

        public AppContextFilterAttribute(IAppContext appContext)
        {
            _appContext = appContext;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            _appContext.SetContext(context.HttpContext.Request);
            base.OnActionExecuting(context);
        }
    }
}
