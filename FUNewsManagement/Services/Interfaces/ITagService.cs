using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccessObjects;

namespace FUNewsManagement.Services
{
    public interface ITagService
    {
        Task<List<Tag>> GetAllAsync();
    }
}
