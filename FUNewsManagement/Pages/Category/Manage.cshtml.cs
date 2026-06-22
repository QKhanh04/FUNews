using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using FUNewsManagement.Services;
using DataAccessObjects;
using ViewModel.Category;

namespace FUNewsManagement.Pages.Category
{
    [Authorize(Roles = "1")] // Staff only
    public class ManageModel : PageModel
    {
        private readonly ICategoryService _categoryService;

        public ManageModel(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        public CategoryManagementViewModel CategoryData { get; set; } = default!;

        [BindProperty]
        public DataAccessObjects.Category CategoryForm { get; set; } = new();

        public async Task OnGetAsync(string? search, int pageNumber = 1, int pageSize = 10)
        {
            CategoryData = await _categoryService.GetCategoryManagementAsync(search, pageNumber, pageSize);
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                TempData["ToastMessage"] = "Validation failed: " + errors;
                TempData["ToastType"] = "danger";
                return RedirectToPage();
            }

            var result = await _categoryService.AddCategoryAsync(CategoryForm);
            TempData["ToastMessage"] = result.Message;
            TempData["ToastType"] = result.IsSuccess ? "success" : "danger";

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEditAsync()
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                TempData["ToastMessage"] = "Validation failed: " + errors;
                TempData["ToastType"] = "danger";
                return RedirectToPage();
            }

            var result = await _categoryService.UpdateCategoryAsync(CategoryForm);
            TempData["ToastMessage"] = result.Message;
            TempData["ToastType"] = result.IsSuccess ? "success" : "danger";

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(short id)
        {
            var result = await _categoryService.DeleteCategoryAsync(id);
            TempData["ToastMessage"] = result.Message;
            TempData["ToastType"] = result.IsSuccess ? "success" : "danger";

            return RedirectToPage();
        }
    }
}
