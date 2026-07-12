using System;
using System.Linq.Expressions;

namespace AcgFotos.Core.Domain
{
    public abstract class Specification<E> : ISpecification<E> where E : class, IEntityBase, new()
    {
        public bool IsSatisfiedBy(E entity)
        {
            var predicate = this.ToExpression().Compile();
            return predicate(entity);
        }

        public abstract Expression<Func<E, bool>> ToExpression();
    }
}
