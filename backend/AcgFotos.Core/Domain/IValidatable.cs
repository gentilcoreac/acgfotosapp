using FluentValidation.Results;
using System.Collections.Generic;

namespace AcgFotos.Core.Domain
{
    public interface IValidatable
    {
        ValidationResult Validate(IValidator<EntityBase> validator);
    }
}
