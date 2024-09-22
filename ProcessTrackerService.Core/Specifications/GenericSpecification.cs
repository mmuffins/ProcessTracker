using System.Linq.Expressions;

namespace ProcessTrackerService.Core.Specifications
{
    public class GenericSpecification<T> : BaseSpecification<T>
    {
        public GenericSpecification(Expression<Func<T, bool>> criteria, Expression<Func<T, object>> orderByExpression, string sort = "desc", string includeStrings = "", bool noTracking = false,
            Expression<Func<T, T>> select = null, int take = 0) : base(criteria)
        {
            if (sort == "desc")
                ApplyOrderByDescending(orderByExpression);
            else
                ApplyOrderBy(orderByExpression);

            if (!string.IsNullOrEmpty(includeStrings))
                AddIncludeRange(includeStrings.Trim().Split(',').ToList());

            if (noTracking)
                AsNoTracking();

            if (select != null)
                Select(select);

            if (take > 0)
                ApplyPaging(0, take);
        }
        public GenericSpecification(Expression<Func<T, bool>> criteria, string includeStrings, bool noTracking = false) : base(criteria)
        {
            if (!string.IsNullOrEmpty(includeStrings))
                AddIncludeRange(includeStrings.Trim().Split(',').ToList());
            if (noTracking)
                AsNoTracking();
        }
        public GenericSpecification(Expression<Func<T, bool>> criteria, Expression<Func<T, T>> select = null, bool noTracking = false) : base(criteria)
        {
            if (select != null)
                Select(select);

            if (noTracking)
                AsNoTracking();
        }
    }
}
