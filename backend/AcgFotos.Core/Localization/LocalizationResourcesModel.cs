using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Text.Json;

namespace AcgFotos.Core.Localization
{
    public class LocalizationResourcesModel
    {
        public LocalizationResourcesModel() {
            this.Translations = [];
        }
        public JsonObject Translations { get; set; }
    }

    public class LocalizationResourcesDto
    {
        public LocalizationResourcesDto()
        {
            this.Translations = new Dictionary<string, string>();
        }

        public LocalizationResourcesDto(LocalizationResourcesModel model)
        {
            this.Translations = new Dictionary<string, string>();

            if (model?.Translations != null)
            {
                string jsonString = model.Translations.ToJsonString();
                this.Translations = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonString)
                    ?? new Dictionary<string, string>();
            }
        }

        public Dictionary<string, string> Translations { get; set; }
    }
}


