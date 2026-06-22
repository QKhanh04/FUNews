using System.Collections.Generic;
using System.Threading.Tasks;
using Common;
using DataAccessObjects;
using ViewModel.Category;

namespace FUNewsManagement.Services
{
    public interface ICategoryService
    {
        Task<IEnumerable<Category>> GetAllActiveCategories();
        Task<IEnumerable<Category>> GetAllCategoriesAsync();
        Task<CategoryManagementViewModel> GetCategoryManagementAsync(string? search, int pageNumber, int pageSize);
        Task<List<CategoryNodeViewModel>> GetCategoryTreeAsync();
        Task<Category?> GetCategoryByIdAsync(short id);
        Task<ServiceResult<bool>> AddCategoryAsync(Category category);
        Task<ServiceResult<bool>> UpdateCategoryAsync(Category category);
        Task<ServiceResult<bool>> DeleteCategoryAsync(short id);
    }
}
