using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using ProcessTrackerService.Core.Interfaces;
using ProcessTrackerService.Infrastructure.Data;
using System.Linq.Expressions;

namespace ProcessTrackerService.Infrastructure.Repository
{
    public class EfRepository<T> : IAsyncRepository<T> where T : class, IAggregateRoot
    {
        protected readonly PTServiceContext _dbContext;

        public EfRepository(PTServiceContext dbContext)
        {
            _dbContext = dbContext;
        }

        public virtual async Task<T> GetByIdAsync(int id)
        {
            return await _dbContext.Set<T>().FindAsync(id);
        }

        public virtual async Task<T> GetByIdAsync(long id)
        {
            return await _dbContext.Set<T>().FindAsync(id);
        }
        public virtual async Task<T> GetByIdAsync(string id)
        {
            return await _dbContext.Set<T>().FindAsync(id);
        }
        public virtual async Task<T> GetByIdAsync(short id)
        {
            return await _dbContext.Set<T>().FindAsync(id);
        }

        public async Task<IReadOnlyList<T>> ListAllAsync()
        {
            return await _dbContext.Set<T>().ToListAsync();
        }

        public async Task<IReadOnlyList<T>> FromSqlRawList(string query)
        {
            var result = await _dbContext.Set<T>().FromSqlRaw(query).ToListAsync();
            return result;

        }
        public async Task<IReadOnlyList<T>> FromSqlRawList(string query, params object[] paramterts)
        {
            var result = await _dbContext.Set<T>().FromSqlRaw(query, paramterts).ToListAsync();
            return result;

        }
        public async Task<T> FromSqlRaw(string query)
        {
            var result = await _dbContext.Set<T>().FromSqlRaw(query).ToListAsync();
            return result.FirstOrDefault();

        }
        public async Task<T> FromSqlRaw(string query, params object[] paramterts)
        {
            var result = await _dbContext.Set<T>().FromSqlRaw(query, paramterts).ToListAsync();
            return result.FirstOrDefault();

        }
        public async Task<int> ExecuteSqlRawAsync(string query)
        {
            return await _dbContext.Database.ExecuteSqlRawAsync(query);
        }
        public async Task<IReadOnlyList<T>> ListAsync(ISpecification<T> spec)
        {
            return await ApplySpecification(spec).ToListAsync();
        }
        public async Task<T> FirstOrDefaultAsync(ISpecification<T> spec)
        {
            return await ApplySpecification(spec).FirstOrDefaultAsync();
        }
        public async Task<bool> AnyAsync(ISpecification<T> spec)
        {
            return await ApplySpecification(spec).AnyAsync();
        }
        public async Task<int> CountAsync(ISpecification<T> spec)
        {
            return await ApplySpecification(spec).CountAsync();
        }
        public async Task<decimal> SumAsync(ISpecification<T> spec, Expression<Func<T, decimal>> sumSpec)
        {
            return await ApplySpecification(spec).SumAsync(sumSpec);
        }

        public async Task<T> AddAsync(T entity)
        {
            _dbContext.Set<T>().Add(entity);
            await _dbContext.SaveChangesAsync();

            return entity;
        }
        public async Task AddRangeAsync(List<T> entity)
        {
            _dbContext.Set<T>().AddRange(entity);
            await _dbContext.SaveChangesAsync().ConfigureAwait(false);
        }
        public async Task UpdateRangeAsync(List<T> entity)
        {
            _dbContext.Set<T>().UpdateRange(entity);
            await _dbContext.SaveChangesAsync().ConfigureAwait(false);
        }
        public async Task<bool> UpdateAsync(T entity)
        {
            _dbContext.Entry(entity).State = EntityState.Modified;
            await _dbContext.SaveChangesAsync().ConfigureAwait(false);
            return true;
        }


        public async Task<bool> UpdateAsync(object primaryKey, T entity)
        {
            var previous = _dbContext.Set<T>().Find(primaryKey);
            if (previous != null)
            {
                _dbContext.Entry(previous).CurrentValues.SetValues(entity);
                var check = await _dbContext.SaveChangesAsync().ConfigureAwait(false);
                return true;
            }
            return false;
        }

        public async Task DeleteAsync(T entity)
        {
            _dbContext.Set<T>().Remove(entity);
            await _dbContext.SaveChangesAsync().ConfigureAwait(false);
        }
        public async Task DeleteRangeAsync(ICollection<T> entity)
        {
            _dbContext.Set<T>().RemoveRange(entity);
            await _dbContext.SaveChangesAsync().ConfigureAwait(false);
        }
        public async Task AddBulkAsync(List<T> entityList)
        {
            try
            {
                _dbContext.ChangeTracker.AutoDetectChangesEnabled = false;
                await _dbContext.BulkInsertAsync(entityList).ConfigureAwait(false);
            }
            finally
            {
                _dbContext.ChangeTracker.AutoDetectChangesEnabled = true;
            }
        }
        public async Task UpdateBulkAsync(List<T> entityList)
        {
            try
            {
                _dbContext.ChangeTracker.AutoDetectChangesEnabled = false;
                await _dbContext.BulkUpdateAsync(entityList).ConfigureAwait(false);
            }
            finally
            {
                _dbContext.ChangeTracker.AutoDetectChangesEnabled = true;
            }
        }
        public async Task AddOrUpdateBulkAsync(List<T> entityList)
        {
            try
            {
                _dbContext.ChangeTracker.AutoDetectChangesEnabled = false;
                await _dbContext.BulkInsertOrUpdateAsync(entityList).ConfigureAwait(false);
            }
            finally
            {
                _dbContext.ChangeTracker.AutoDetectChangesEnabled = true;
            }
        }

        public async Task DeleteBulkAsync(List<T> entityList)
        {
            try
            {
                _dbContext.ChangeTracker.AutoDetectChangesEnabled = false;
                await _dbContext.BulkDeleteAsync(entityList).ConfigureAwait(false);
            }
            finally
            {
                _dbContext.ChangeTracker.AutoDetectChangesEnabled = true;
            }
        }
        private IQueryable<T> ApplySpecification(ISpecification<T> spec)
        {
            return SpecificationEvaluator<T>.GetQuery(_dbContext.Set<T>().AsQueryable(), spec);
        }
    }
}
