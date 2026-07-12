using System.ComponentModel.DataAnnotations.Schema;
using FluentValidation.Results;

namespace AcgFotos.Core.Domain
{
    public abstract class EntityBase : IEntityBase, IValidatable
    {
        public long Id { get; set; }

        public ValidationResult Validate(IValidator<EntityBase> validator)
        {
            return validator.Validate(this);
        }
    }
}
