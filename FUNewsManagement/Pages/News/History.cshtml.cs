using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using FUNewsManagement.Services;
using ViewModel.News;
using System.Security.Claims;

namespace FUNewsManagement.Pages.News
{
    [Authorize(Roles = "1")] // Staff only
    public class HistoryModel : PageModel
    {
        private readonly INewsService _newsService;
        private readonly ICategoryService _categoryService;

        public HistoryModel(INewsService newsService, ICategoryService categoryService)
        {
            _newsService = newsService;
            _categoryService = categoryService;
        }

        public NewsHistoryViewModel HistoryData { get; set; } = default!;
        public IEnumerable<DataAccessObjects.Category> Categories { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(
            string? search,
            short? categoryId,
            DateTime? startDate,
            DateTime? endDate,
            int pageNumber = 1,
            int pageSize = 10)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!short.TryParse(userIdStr, out short userId))
                return RedirectToPage("/Account/Login");

            HistoryData = await _newsService.GetHistoryAsync(
                userId, search, categoryId, startDate, endDate, pageNumber, pageSize);

            Categories = await _categoryService.GetAllActiveCategories();

            return Page();
        }
    }
}
