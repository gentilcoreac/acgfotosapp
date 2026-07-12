using System;
using System.Linq.Expressions;

namespace AcgFotos.Core.Domain
{
    public interface ISpecification<E> where E : class, IEntityBase, new()
    {
        bool IsSatisfiedBy(E entity);

        Expression<Func<E, bool>> ToExpression();
    }
}
