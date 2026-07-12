using Microsoft.AspNetCore.Http;

namespace AcgFotos.Core.Session
{
    public class RequestHeaderHandler
    {
        public static AppContext Encode(HttpRequest request)
        {
            if (request != null)
            {
                var dataContext = new AppContext();
                dataContext.SetContext(request);
                return dataContext;
            }
            else
            {
                return null;
            }
        }
    }
}
