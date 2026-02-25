using System.Diagnostics;
using System.Threading.Tasks;
using FUNewsManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Service.Interface;
using ViewModel.Home;

namespace FUNewsManagement.Controllers
{
    public class HomeController : Controller
    {
        private readonly INewsService _newsService;
        private readonly ICategoryService _categoryService;
        private const int PageSize = 6;

        public HomeController(INewsService newsService, ICategoryService categoryService)
        {
            _newsService = newsService;
            _categoryService = categoryService;
        }

        // GET: /Home/Index
        // Supports: ?search=...&categoryId=...&tagId=...&cursor=...&incSubs=...
        public async Task<IActionResult> Index(
            string? search,
            short? categoryId,
            int? tagId,
            int? cursor)
        {
            ViewData["CurrentSearch"] = search;
            
            var categories = await _categoryService.GetAllActiveCategories();

            // 2. Load news with cursor pagination
            var grid = await _newsService.GetForHomeAsync(search, categoryId, tagId, cursor, PageSize + 1);

            // 3. Separate featured article (first item, only on first page / no cursor / no filter)
            NewsCardViewModel? featured = null;
            var gridItems = grid.Items.ToList();

            if (cursor == null && string.IsNullOrWhiteSpace(search) && !categoryId.HasValue && !tagId.HasValue)
            {
                featured = gridItems.FirstOrDefault();
                gridItems = gridItems.Skip(1).ToList();
            }

            var vm = new HomeIndexViewModel
            {
                FeaturedArticle = featured,
                Grid = new Common.CursorResult<NewsCardViewModel>
                {
                    Items = gridItems,
                    NextCursor = grid.NextCursor,
                    HasMore = grid.HasMore
                },
                Categories = categories,
                SelectedCategoryId = categoryId,
                SelectedTagId = tagId,
                SearchKeyword = search
            };

            return View(vm);
        }

        // GET: /Home/LoadMore  (AJAX / partial)
        // Returns only the article card partial for infinite scroll / "Show More"
        [HttpGet]
        public async Task<IActionResult> LoadMore(
            string? search,
            short? categoryId,
            int? tagId,
            int? cursor)
        {
            var result = await _newsService.GetForHomeAsync(search, categoryId, tagId, cursor, PageSize);

            ViewBag.NextCursor = result.NextCursor;
            ViewBag.HasMore = result.HasMore;
            ViewBag.Search = search;
            ViewBag.CategoryId = categoryId;

            return PartialView("_NewsCardPartial", result.Items);
        }

        // GET: /Home/TagDrawerArticles (AJAX / partial)
        [HttpGet]
        public async Task<IActionResult> TagDrawerArticles(int tagId)
        {
            var result = await _newsService.GetForHomeAsync(null, null, tagId, null, 5);
            return PartialView("_TagDrawerPartial", result.Items.ToList());
        }

        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() => View();
    }
}
