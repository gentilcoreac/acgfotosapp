using FluentValidation;
using System.Globalization;

namespace AcgFotos.Core.Application
{
    public abstract class CustomAbstractValidator<T> : AbstractValidator<T>
    {
        public CustomAbstractValidator()
        {
            // https://docs.fluentvalidation.net/en/latest/localization.html
            ValidatorOptions.Global.LanguageManager.Culture = new CultureInfo("es-AR");
        }
    }
}
