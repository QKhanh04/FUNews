using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using FUNewsManagement.Services;
using ViewModel.Home;

namespace FUNewsManagement.Pages.News
{
    public class DetailModel : PageModel
    {
        private readonly INewsService _newsService;

        public DetailModel(INewsService newsService)
        {
            _newsService = newsService;
        }

        public NewsDetailViewModel VM { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            // 1. Load the specific article
            var article = await _newsService.GetNewsByIdAsync(id);

            if (article == null)
                return NotFound();

            // Prevent guests from viewing drafts
            var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            if (article.NewsStatus != true && role != "0" && role != "1")
                return NotFound();

            // 2. Related articles: same category, active only, exclude current, take 3
            var allActiveNews = await _newsService.GetAllNewsActive();
            var related = allActiveNews
                .Where(n => n.NewsArticleId != id &&
                            n.CategoryId == article.CategoryId)
                .OrderByDescending(n => n.CreatedDate)
                .Take(3)
                .Select(n => new NewsCardViewModel
                {
                    NewsArticleId = n.NewsArticleId,
                    NewsTitle = n.NewsTitle ?? string.Empty,
                    Headline = n.Headline ?? string.Empty,
                    NewsContent = n.NewsContent,
                    CreatedDate = n.CreatedDate,
                    CategoryName = n.Category != null ? n.Category.CategoryName : null,
                    ThumbnailUrl = n.ThumbnailUrl
                })
                .ToList();

            // 3. Build ViewModel
            VM = new NewsDetailViewModel
            {
                NewsArticleId = article.NewsArticleId,
                NewsTitle = article.NewsTitle ?? string.Empty,
                Headline = article.Headline ?? string.Empty,
                NewsContent = article.NewsContent,
                CreatedDate = article.CreatedDate,
                CategoryName = article.Category != null ? article.Category.CategoryName : null,
                CategoryId = article.CategoryId,
                ThumbnailUrl = article.ThumbnailUrl,
                CreatedByName = article.CreatedBy?.AccountName ?? "System",
                CreatedByRole = article.CreatedBy?.AccountRole == 0 ? "Admin" : (article.CreatedBy?.AccountRole == 1 ? "Staff" : "Lecturer"),
                Tags = article.Tags?.ToDictionary(t => t.TagId, t => t.TagName ?? string.Empty) 
                       ?? new Dictionary<int, string>(),
                RelatedArticles = related
            };

            return Page();
        }
    }
}
