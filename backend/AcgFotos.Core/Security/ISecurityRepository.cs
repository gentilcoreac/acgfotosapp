using System.Collections.Generic;

namespace AcgFotos.Core.Security
{
    public interface ISecurityRepository
    {
        public void RegisterEndpoints(List<EndpointDto> endpoints);

        public List<EndpointDto> GetAll(string userName, long tenantID);

        bool ValidateSecurityStamp(string userName, long tenantId, string securityStamp);
    }
}
