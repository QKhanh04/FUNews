using Microsoft.AspNetCore.Mvc;
using Service.Interface;
using ViewModel.Home;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Linq;
using System.Threading.Tasks;

namespace FUNewsManagement.Controllers
{
    public class NewsController : Controller
    {
        private readonly INewsService _newsService;
        private readonly ICategoryService _categoryService;
        private readonly ITagService _tagService;

        public NewsController(INewsService newsService, ICategoryService categoryService, ITagService tagService)
        {
            _newsService = newsService;
            _categoryService = categoryService;
            _tagService = tagService;
        }

        // GET: /News/Detail/5
        public async Task<IActionResult> Detail(int id)
        {
            // 1. Load all active news
            var allNews = await _newsService.GetAllNewsActive();
            var article = allNews.FirstOrDefault(n => n.NewsArticleId == id);

            if (article == null)
                return NotFound();

            // 2. Related articles: same category, exclude current, take 3
            var related = allNews
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
            var vm = new NewsDetailViewModel
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

            ViewData["CurrentSearch"] = string.Empty;
            return View(vm);
        }

        [Authorize(Roles = "1,0")] // Staff = 1, Admin = 0
        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!short.TryParse(userIdStr, out short userId))
                return RedirectToAction("Login", "Account");

            // Reusing history stats logic for dashboard summary
            var history = await _newsService.GetHistoryAsync(
                userId, null, null, null, null, 1, 1);
            
            return View(history);
        }

        [Authorize(Roles = "1,0")]
        [HttpGet]
        public async Task<IActionResult> Manage(string? search, short? categoryId, bool? status, int pageNumber = 1, int pageSize = 10)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            if (!short.TryParse(userIdStr, out short userId))
                return RedirectToAction("Login", "Account");

            var result = await _newsService.GetNewsManagementAsync(userId, role ?? "1", search, categoryId, status, pageNumber, pageSize);
            ViewBag.Categories = await _categoryService.GetAllCategoriesAsync();
            ViewBag.AllTags = await _tagService.GetAllAsync();
            
            return View(result);
        }

        [Authorize(Roles = "1,0")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DataAccessObjects.NewsArticle article, List<int> selectedTagIds, string? newTags, Microsoft.AspNetCore.Http.IFormFile? ThumbnailFile)
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                TempData["ToastMessage"] = "Validation Error: " + errors;
                TempData["ToastType"] = "danger";
                return RedirectToAction("Manage");
            }

            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (short.TryParse(userIdStr, out short userId) && userId > 0)
            {
                article.CreatedById = userId;
                article.UpdatedById = userId;
            }
            else
            {
                article.CreatedById = null;
                article.UpdatedById = null;
            }
            
            var result = await _newsService.AddNewsAsync(article, selectedTagIds, newTags, ThumbnailFile);
            if (result.IsSuccess)
            {
                TempData["ToastMessage"] = result.Message;
                TempData["ToastType"] = "success";
            }
            else
            {
                TempData["ToastMessage"] = result.Message;
                TempData["ToastType"] = "danger";
            }
            return RedirectToAction("Manage");
        }

        [Authorize(Roles = "1,0")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(DataAccessObjects.NewsArticle article, List<int> selectedTagIds, string? newTags, Microsoft.AspNetCore.Http.IFormFile? ThumbnailFile)
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                TempData["ToastMessage"] = "Validation Error: " + errors;
                TempData["ToastType"] = "danger";
                return RedirectToAction("Manage");
            }

            var result = await _newsService.UpdateNewsAsync(article, selectedTagIds, newTags, ThumbnailFile);
            if (result.IsSuccess)
            {
                TempData["ToastMessage"] = result.Message;
                TempData["ToastType"] = "success";
            }
            else
            {
                TempData["ToastMessage"] = result.Message;
                TempData["ToastType"] = "danger";
            }
            return RedirectToAction("Manage");
        }

        [Authorize(Roles = "1,0")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _newsService.DeleteNewsAsync(id);
            if (result.IsSuccess)
            {
                TempData["ToastMessage"] = result.Message;
                TempData["ToastType"] = "success";
            }
            else
            {
                TempData["ToastMessage"] = result.Message;
                TempData["ToastType"] = "error";
            }
            return RedirectToAction("Manage");
        }

        [Authorize(Roles = "1,0")] // Staff = 1, Admin = 0
        [HttpGet]
        public async Task<IActionResult> History(
            string? search,
            short? categoryId,
            DateTime? startDate,
            DateTime? endDate,
            int pageNumber = 1,
            int pageSize = 10)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!short.TryParse(userIdStr, out short userId))
                return RedirectToAction("Login", "Account");

            var history = await _newsService.GetHistoryAsync(
                userId, search, categoryId, startDate, endDate, pageNumber, pageSize);

            var categories = await _categoryService.GetAllActiveCategories();
            ViewBag.Categories = categories;

            return View(history);
        }

        [Authorize(Roles = "0")] // Admin only
        [HttpGet]
        public async Task<IActionResult> Report(DateTime? startDate, DateTime? endDate, int pageNumber = 1, int pageSize = 10)
        {
            var report = await _newsService.GetReportAsync(startDate, endDate, pageNumber, pageSize);
            return View(report);
        }
    }
}