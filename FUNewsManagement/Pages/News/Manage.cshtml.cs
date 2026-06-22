using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using FUNewsManagement.Services;
using DataAccessObjects;
using ViewModel.News;
using System.Security.Claims;

namespace FUNewsManagement.Pages.News
{
    [Authorize(Roles = "0,1")]
    public class ManageModel : PageModel
    {
        private readonly INewsService _newsService;
        private readonly ICategoryService _categoryService;
        private readonly ITagService _tagService;

        public ManageModel(INewsService newsService, ICategoryService categoryService, ITagService tagService)
        {
            _newsService = newsService;
            _categoryService = categoryService;
            _tagService = tagService;
        }

        public NewsManagementViewModel NewsData { get; set; } = default!;
        public IEnumerable<DataAccessObjects.Category> Categories { get; set; } = default!;
        public IEnumerable<DataAccessObjects.Tag> AllTags { get; set; } = default!;

        [BindProperty]
        public DataAccessObjects.NewsArticle ArticleForm { get; set; } = new();

        [BindProperty]
        public List<int> SelectedTagIds { get; set; } = new();

        [BindProperty]
        public string? NewTags { get; set; }

        [BindProperty]
        public IFormFile? ThumbnailFile { get; set; }

        public async Task OnGetAsync(string? search, short? categoryId, bool? status, int pageNumber = 1, int pageSize = 10)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            short.TryParse(userIdStr, out short userId);

            NewsData = await _newsService.GetNewsManagementAsync(userId, role ?? "1", search, categoryId, status, pageNumber, pageSize);
            Categories = await _categoryService.GetAllCategoriesAsync();
            AllTags = await _tagService.GetAllAsync();
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                TempData["ToastMessage"] = "Validation Error: " + errors;
                TempData["ToastType"] = "danger";
                return RedirectToPage();
            }

            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (short.TryParse(userIdStr, out short userId) && userId > 0)
            {
                ArticleForm.CreatedById = userId;
                ArticleForm.UpdatedById = userId;
            }

            var result = await _newsService.AddNewsAsync(ArticleForm, SelectedTagIds, NewTags, ThumbnailFile, userRole);
            TempData["ToastMessage"] = result.Message;
            TempData["ToastType"] = result.IsSuccess ? "success" : "danger";

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEditAsync()
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                TempData["ToastMessage"] = "Validation Error: " + errors;
                TempData["ToastType"] = "danger";
                return RedirectToPage();
            }

            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            var result = await _newsService.UpdateNewsAsync(ArticleForm, SelectedTagIds, NewTags, ThumbnailFile, userRole);
            TempData["ToastMessage"] = result.Message;
            TempData["ToastType"] = result.IsSuccess ? "success" : "danger";

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var result = await _newsService.DeleteNewsAsync(id);
            TempData["ToastMessage"] = result.Message;
            TempData["ToastType"] = result.IsSuccess ? "success" : "danger";

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostApproveAsync(int id)
        {
            var result = await _newsService.ApproveNewsAsync(id);
            TempData["ToastMessage"] = result.Message;
            TempData["ToastType"] = result.IsSuccess ? "success" : "danger";

            return RedirectToPage();
        }
    }
}
