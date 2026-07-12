using System.Globalization;
using System.Resources;

namespace AcgFotos.Core.Localization
{
    public class LocalizationResourcesManager : ILocalizationResourcesManager
    {
        public LocalizationResourcesModel GetAllResourcesByCultureInfo(string cultureInfoName, long appId, bool isRoot = false, bool includePrivateResources = false)
        {
            var localizationResourcesModel = new LocalizationResourcesModel();

            var cultureInfo = new CultureInfo(cultureInfoName);

            this.AddLocalizationResources(ref localizationResourcesModel, PublicResources.Fields.Fields.ResourceManager.GetResourceSet(cultureInfo, true, true));
            this.AddLocalizationResources(ref localizationResourcesModel, PublicResources.Texts.Texts.ResourceManager.GetResourceSet(cultureInfo, true, true));
            this.AddLocalizationResources(ref localizationResourcesModel, PublicResources.Messages.Messages.ResourceManager.GetResourceSet(cultureInfo, true, true));

            if (includePrivateResources)
            {
                //Fields comunes a las dos aplicaciones
                this.AddLocalizationResources(ref localizationResourcesModel, PrivateResources.General.Fields.Fields.ResourceManager.GetResourceSet(cultureInfo, true, true));
                this.AddLocalizationResources(ref localizationResourcesModel, PrivateResources.General.Messages.Messages.ResourceManager.GetResourceSet(cultureInfo, true, true));
                this.AddLocalizationResources(ref localizationResourcesModel, PrivateResources.General.Texts.Texts.ResourceManager.GetResourceSet(cultureInfo, true, true));

                if (appId == 1)
                {
                    this.AddLocalizationResources(ref localizationResourcesModel, PrivateResources.Database.Fields.Fields.ResourceManager.GetResourceSet(cultureInfo, true, true));
                    this.AddLocalizationResources(ref localizationResourcesModel, PrivateResources.Database.Messages.Messages.ResourceManager.GetResourceSet(cultureInfo, true, true));

                    this.AddLocalizationResources(ref localizationResourcesModel, PrivateResources.Webapp.Fields.Fields.ResourceManager.GetResourceSet(cultureInfo, true, true));
                    this.AddLocalizationResources(ref localizationResourcesModel, PrivateResources.Webapp.Texts.Texts.ResourceManager.GetResourceSet(cultureInfo, true, true));
                    this.AddLocalizationResources(ref localizationResourcesModel, PrivateResources.Webapp.Messages.Messages.ResourceManager.GetResourceSet(cultureInfo, true, true));

                    if (isRoot) {
                        this.AddLocalizationResources(ref localizationResourcesModel, PrivateResources.WebappRoot.Fields.Fields.ResourceManager.GetResourceSet(cultureInfo, true, true));                     
                        this.AddLocalizationResources(ref localizationResourcesModel, PrivateResources.WebappRoot.Messages.Messages.ResourceManager.GetResourceSet(cultureInfo, true, true));
                    }
                
                }
                if (appId == 2)
                {
                    this.AddLocalizationResources(ref localizationResourcesModel, PrivateResources.Database.Fields.Fields.ResourceManager.GetResourceSet(cultureInfo, true, true));

                    this.AddLocalizationResources(ref localizationResourcesModel, PrivateResources.Office.Fields.Fields.ResourceManager.GetResourceSet(cultureInfo, true, true));
                    this.AddLocalizationResources(ref localizationResourcesModel, PrivateResources.Office.Messages.Messages.ResourceManager.GetResourceSet(cultureInfo, true, true));
                }
            }

            return localizationResourcesModel;
        }

        private void AddLocalizationResources(ref LocalizationResourcesModel localizationResourcesModel, ResourceSet resourceSet)
        {
            var enumerator = resourceSet.GetEnumerator();
            while (enumerator.MoveNext())
            {
                if (!localizationResourcesModel.Translations.ContainsKey(enumerator.Key.ToString()))
                {
                    localizationResourcesModel.Translations.Add(enumerator.Key.ToString(), enumerator.Value.ToString());
                }
            }
        }
    }
}
