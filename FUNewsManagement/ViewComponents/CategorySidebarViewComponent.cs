using Microsoft.AspNetCore.Mvc;
using FUNewsManagement.Services;
using System.Threading.Tasks;

namespace FUNewsManagement.ViewComponents
{
    public class CategorySidebarViewComponent : ViewComponent
    {
        private readonly ICategoryService _categoryService;

        public CategorySidebarViewComponent(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        public async Task<IViewComponentResult> InvokeAsync(short? activeCategoryId = null)
        {
            var categories = await _categoryService.GetCategoryTreeAsync();
            ViewData["ActiveCategoryId"] = activeCategoryId;
            return View(categories);
        }
    }
}
