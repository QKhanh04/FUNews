using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repository.Interface
{
    public interface IGenericRepository<T, TKey> where T : class
    {
        Task<T?> GetByIdAsync(TKey id);
        Task<List<T>> GetAllAsync();
        Task AddAsync(T entity);
        void Update(T entity);
        void Remove(T entity);
        IQueryable<T> GetAllAsQueryable();
    }
}

