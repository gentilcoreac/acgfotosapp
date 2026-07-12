using FluentValidation.Results;

namespace AcgFotos.Core.Domain
{
    public abstract class BusinessValidator<E> : IValidator<E> where E : class, IEntityBase, new()
    {
        protected ValidationResult ValidationResult { get; private set; }

        public BusinessValidator()
        {
            this.ValidationResult = new ValidationResult();
        }

        public abstract ValidationResult Validate(E entity);
    }
}
