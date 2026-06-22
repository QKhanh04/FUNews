using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using FUNewsManagement.Services;
using ViewModel.Home;

namespace FUNewsManagement.Pages
{
    public class IndexModel : PageModel
    {
        private readonly INewsService _newsService;
        private readonly ICategoryService _categoryService;
        private const int PageSize = 6;

        public IndexModel(INewsService newsService, ICategoryService categoryService)
        {
            _newsService = newsService;
            _categoryService = categoryService;
        }

        public HomeIndexViewModel VM { get; set; } = default!;

        public async Task OnGetAsync(string? search, short? categoryId, int? tagId, int? cursor)
        {
            ViewData["CurrentSearch"] = search;
            var categories = await _categoryService.GetAllActiveCategories();
            var grid = await _newsService.GetForHomeAsync(search, categoryId, tagId, cursor, PageSize + 1);

            NewsCardViewModel? featured = null;
            var gridItems = grid.Items.ToList();

            if (cursor == null && string.IsNullOrWhiteSpace(search) && !categoryId.HasValue && !tagId.HasValue)
            {
                featured = gridItems.FirstOrDefault();
                gridItems = gridItems.Skip(1).ToList();
            }

            VM = new HomeIndexViewModel
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
        }

        public async Task<IActionResult> OnGetLoadMoreAsync(string? search, short? categoryId, int? tagId, int? cursor)
        {
            var result = await _newsService.GetForHomeAsync(search, categoryId, tagId, cursor, PageSize);
            return Partial("_NewsCardPartial", result.Items);
        }

        public async Task<IActionResult> OnGetTagDrawerArticlesAsync(int tagId)
        {
            var result = await _newsService.GetForHomeAsync(null, null, tagId, null, 5);
            return Partial("_TagDrawerPartial", result.Items);
        }
    }
}
