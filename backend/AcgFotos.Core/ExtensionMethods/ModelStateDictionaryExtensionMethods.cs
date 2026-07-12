using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Linq;

namespace AcgFotos.Core.ExtensionMethods
{
    public static class ModelStateDictionaryExtensionMethods
    {
        public static string[] GetErrors(this ModelStateDictionary modelState)
        {
            var errors = modelState.Keys.SelectMany(k => modelState[k].Errors).
                                               Where(x => !string.IsNullOrEmpty(x.ErrorMessage)).
                                               Select(m => m.ErrorMessage).ToArray();
            if (errors.Count() == 0)
            {
                errors = modelState.Keys.Select(x => "Problema con el campo: " + x).ToArray();
            }

            return errors;
        }
    }
}
