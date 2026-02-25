using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Service.Interface;
using System.Threading.Tasks;
using DataAccessObjects;

namespace FUNewsManagement.Controllers
{
    [Authorize(Roles = "1,0")] // Staff and Admin
    public class CategoryController : Controller
    {
        private readonly ICategoryService _categoryService;

        public CategoryController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        [HttpGet]
        public async Task<IActionResult> Manage(string? search, int pageNumber = 1, int pageSize = 10)
        {
            var model = await _categoryService.GetCategoryManagementAsync(search, pageNumber, pageSize);
            return View(model);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category category)
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                TempData["ToastMessage"] = "Validation failed: " + errors;
                TempData["ToastType"] = "danger";
                return RedirectToAction(nameof(Manage));
            }

            var result = await _categoryService.AddCategoryAsync(category);
            TempData["ToastMessage"] = result.Message;
            TempData["ToastType"] = result.IsSuccess ? "success" : "danger";

            return RedirectToAction(nameof(Manage));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(short id)
        {
            var category = await _categoryService.GetCategoryByIdAsync(id);
            if (category == null)
                return NotFound();

            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Category category)
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                TempData["ToastMessage"] = "Validation failed: " + errors;
                TempData["ToastType"] = "danger";
                return RedirectToAction(nameof(Manage));
            }

            var result = await _categoryService.UpdateCategoryAsync(category);
            TempData["ToastMessage"] = result.Message;
            TempData["ToastType"] = result.IsSuccess ? "success" : "danger";

            return RedirectToAction(nameof(Manage));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(short id)
        {
            var result = await _categoryService.DeleteCategoryAsync(id);
            TempData["ToastMessage"] = result.Message;
            TempData["ToastType"] = result.IsSuccess ? "success" : "danger";

            return RedirectToAction(nameof(Manage));
        }
    }
}
