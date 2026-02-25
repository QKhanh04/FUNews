using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccessObjects;
using Microsoft.EntityFrameworkCore;
using Repository.Interface;

namespace Repository.Implement
{
    public class GenericRepository<T, Tkey> : IGenericRepository<T, Tkey> where T : class
    {
        protected readonly FUNewsContext _context;
        private readonly DbSet<T> _dbSet;
        public GenericRepository(FUNewsContext context)
        {
            _context = context;
            _dbSet = _context.Set<T>();
        }
        public async Task<T?> GetByIdAsync(Tkey id) => await _dbSet.FindAsync(id);
        public async Task<List<T>> GetAllAsync() => await _dbSet.ToListAsync();
        public async Task AddAsync(T entity) => await _dbSet.AddAsync(entity);

        public void Remove(T entity) => _dbSet.Remove(entity);

        public void Update(T entity) => _dbSet.Update(entity);
        public IQueryable<T> GetAllAsQueryable() => _dbSet;

    }
}
