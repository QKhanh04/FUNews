using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccessObjects;

namespace Repository.Interface
{
    public interface INewsRepository : IGenericRepository<NewsArticle, int>
    {
        Task<List<NewsArticle>> GetAllNewsAsync();
        IQueryable<NewsArticle> GetActiveQueryable();

    }
}
