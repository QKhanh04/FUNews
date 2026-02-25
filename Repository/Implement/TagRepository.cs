using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccessObjects;
using Repository.Interface;

namespace Repository.Implement
{
    public class TagRepository : GenericRepository<Tag, int>, ITagRepository
    {
        public TagRepository(FUNewsContext context) : base(context)
        {
        }
    }
}
