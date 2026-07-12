using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AcgFotos.Core.Localization
{
    public interface ILocalizationResourcesManager
    {
        public LocalizationResourcesModel GetAllResourcesByCultureInfo(string cultureInfoName, long appId, bool isRoot = false, bool includePrivateResources = false);
    }
}
