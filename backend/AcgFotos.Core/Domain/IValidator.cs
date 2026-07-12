using FluentValidation.Results;
using System;
using System.Collections.Generic;
using System.Text;

namespace AcgFotos.Core.Domain
{
    public interface IValidator<E> where E : class, IEntityBase
    {
        ValidationResult Validate(E entity);

    }
}
