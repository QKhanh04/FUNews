using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccessObjects;
using Repository.Interface;

namespace Repository.Implement
{
    public class CategoryRepository : GenericRepository<Category, short>, ICategoryRepository
    {
        public CategoryRepository(FUNewsContext context) : base(context)
        {
        }
    }
}
