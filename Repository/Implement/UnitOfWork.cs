using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccessObjects;
using Repository.Interface;

namespace Repository.Implement
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly FUNewsContext _context;
        public UnitOfWork(FUNewsContext context)
        {
            _context = context;
        }

        public async Task<int> SaveChangesAsync() => await _context.SaveChangesAsync();

    }
}
