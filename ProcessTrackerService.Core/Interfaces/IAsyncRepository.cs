using System.Linq.Expressions;

namespace ProcessTrackerService.Core.Interfaces
{
    public interface IAsyncRepository<T> where T : class, IAggregateRoot
    {
        Task<T> GetByIdAsync(int id);
        Task<T> GetByIdAsync(long id);
        Task<T> GetByIdAsync(string id);
        Task<T> GetByIdAsync(short id);
        Task<IReadOnlyList<T>> ListAllAsync();
        Task<IReadOnlyList<T>> FromSqlRawList(string query);
        Task<IReadOnlyList<T>> FromSqlRawList(string query, params object[] paramterts);
        Task<T> FromSqlRaw(string query);
        Task<T> FromSqlRaw(string query, params object[] paramterts);
        Task<int> ExecuteSqlRawAsync(string query);
        Task<T> FirstOrDefaultAsync(ISpecification<T> spec);
        Task<bool> AnyAsync(ISpecification<T> spec);
        Task<IReadOnlyList<T>> ListAsync(ISpecification<T> spec);
        Task<T> AddAsync(T entity);
        Task AddRangeAsync(List<T> entity);
        Task UpdateRangeAsync(List<T> entity);
        Task DeleteRangeAsync(ICollection<T> entityList);
        Task<bool> UpdateAsync(T entity);
        Task<bool> UpdateAsync(object primaryKey, T entity);
        Task DeleteAsync(T entity);
        Task AddBulkAsync(List<T> entityList);
        Task UpdateBulkAsync(List<T> entityList);
        Task AddOrUpdateBulkAsync(List<T> entityList);
        Task DeleteBulkAsync(List<T> entityList);
        Task<int> CountAsync(ISpecification<T> spec);
        Task<decimal> SumAsync(ISpecification<T> spec, Expression<Func<T, decimal>> sumSpec);
    }
}
