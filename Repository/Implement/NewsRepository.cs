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
    public class NewsRepository : GenericRepository<NewsArticle, int>, INewsRepository
    {
        public NewsRepository(FUNewsContext context) : base(context)
        {
        }
        public async Task<List<NewsArticle>> GetAllNewsAsync()
        {
            return await _context.NewsArticles
                .Include(n => n.Category)
                .Include(n => n.CreatedBy)
                .Include(n => n.Tags)
                .ToListAsync();
        }

        public IQueryable<NewsArticle> GetActiveQueryable()
        {
            return _context.NewsArticles
                .AsNoTracking()
                .Where(n => n.NewsStatus == true);
        }


    }
}
