using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataAccessObjects;
using Repository.Interface;
using Service.Interface;
using Microsoft.EntityFrameworkCore;
using ViewModel.Category;
using Common;

namespace Service.Implement
{
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CategoryService(ICategoryRepository categoryRepository, IUnitOfWork unitOfWork)
        {
            _categoryRepository = categoryRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<Category>> GetAllActiveCategories()
        {
            var all = await _categoryRepository.GetAllAsync();
            return all.Where(c => c.IsActive == true);
        }

        public async Task<IEnumerable<Category>> GetAllCategoriesAsync()
        {
            return await _categoryRepository.GetAllAsync();
        }

        public async Task<CategoryManagementViewModel> GetCategoryManagementAsync(string? search, int pageNumber, int pageSize)
        {
            IQueryable<Category> query = _categoryRepository.GetAllAsQueryable()
                .Include(c => c.NewsArticles)
                .Include(c => c.ParentCategory);

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(c => 
                    c.CategoryName.Contains(search) || 
                    c.CategoryDesciption.Contains(search));
            }

            var totalCount = await query.CountAsync();
            
            var items = await query
                .OrderBy(c => c.CategoryName)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new CategoryItemViewModel
                {
                    CategoryId = c.CategoryId,
                    CategoryName = c.CategoryName,
                    CategoryDesciption = c.CategoryDesciption,
                    ParentCategoryId = c.ParentCategoryId,
                    ParentCategoryName = c.ParentCategory != null ? c.ParentCategory.CategoryName : "None",
                    IsActive = c.IsActive ?? false,
                    NewsCount = c.NewsArticles.Count
                })
                .ToListAsync();

            // Calculate Stats
            var allCategories = await _categoryRepository.GetAllAsQueryable()
                .Include(c => c.NewsArticles)
                .ToListAsync();

            var stats = new CategoryStatsViewModel
            {
                TotalCount = allCategories.Count,
                ActiveCount = allCategories.Count(c => c.IsActive == true),
                UnusedCount = allCategories.Count(c => !c.NewsArticles.Any()),
                GrowthThisMonth = 0 // Placeholder
            };

            return new CategoryManagementViewModel
            {
                Categories = items,
                Stats = stats,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                SearchTerm = search,
                AllCategories = allCategories.Select(c => new CategoryItemViewModel {
                    CategoryId = c.CategoryId,
                    CategoryName = c.CategoryName
                }).ToList()
            };
        }

        public async Task<List<CategoryNodeViewModel>> GetCategoryTreeAsync()
        {
            var activeCategories = await _categoryRepository.GetAllAsQueryable()
                .Where(c => c.IsActive == true)
                .OrderBy(c => c.CategoryName)
                .ToListAsync();

            var lookup = activeCategories.ToLookup(c => c.ParentCategoryId);

            List<CategoryNodeViewModel> BuildTree(short? parentId)
            {
                return lookup[parentId]
                    .Select(c => new CategoryNodeViewModel
                    {
                        CategoryId = c.CategoryId,
                        CategoryName = c.CategoryName,
                        CategoryDescription = c.CategoryDesciption,
                        SubCategories = BuildTree(c.CategoryId)
                    })
                    .ToList();
            }

            return BuildTree(null);
        }

        public async Task<Category?> GetCategoryByIdAsync(short id)
        {
            return await _categoryRepository.GetByIdAsync(id);
        }

        public async Task<ServiceResult<bool>> AddCategoryAsync(Category category)
        {
            try
            {
                category.CategoryName = category.CategoryName.Trim();
                category.CategoryDesciption = category.CategoryDesciption.Trim();

                var duplicated = await _categoryRepository.GetAllAsQueryable()
                    .AnyAsync(c => c.CategoryName == category.CategoryName);

                if (duplicated)
                {
                    return ServiceResult<bool>.Fail("A category with the same name already exists.");
                }

                if (category.ParentCategoryId.HasValue)
                {
                    var parentExists = await _categoryRepository.GetAllAsQueryable()
                        .AnyAsync(c => c.CategoryId == category.ParentCategoryId.Value);

                    if (!parentExists)
                    {
                        return ServiceResult<bool>.Fail("Selected parent category does not exist.");
                    }
                }

                await _categoryRepository.AddAsync(category);
                await _unitOfWork.SaveChangesAsync();
                return ServiceResult<bool>.Ok(true, "Category added successfully.");
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Fail($"Error adding category: {ex.Message}");
            }
        }

        public async Task<ServiceResult<bool>> UpdateCategoryAsync(Category category)
        {
            try
            {
                var existing = await _categoryRepository.GetByIdAsync(category.CategoryId);
                if (existing == null)
                {
                    return ServiceResult<bool>.Fail("Category not found.");
                }

                category.CategoryName = category.CategoryName.Trim();
                category.CategoryDesciption = category.CategoryDesciption.Trim();

                if (category.ParentCategoryId == category.CategoryId)
                {
                    return ServiceResult<bool>.Fail("A category cannot be its own parent.");
                }

                var duplicated = await _categoryRepository.GetAllAsQueryable()
                    .AnyAsync(c => c.CategoryId != category.CategoryId && c.CategoryName == category.CategoryName);

                if (duplicated)
                {
                    return ServiceResult<bool>.Fail("A category with the same name already exists.");
                }

                if (category.ParentCategoryId.HasValue)
                {
                    var parentExists = await _categoryRepository.GetAllAsQueryable()
                        .AnyAsync(c => c.CategoryId == category.ParentCategoryId.Value);

                    if (!parentExists)
                    {
                        return ServiceResult<bool>.Fail("Selected parent category does not exist.");
                    }
                }

                existing.CategoryName = category.CategoryName;
                existing.CategoryDesciption = category.CategoryDesciption;
                existing.ParentCategoryId = category.ParentCategoryId;
                existing.IsActive = category.IsActive;

                _categoryRepository.Update(existing);
                await _unitOfWork.SaveChangesAsync();
                return ServiceResult<bool>.Ok(true, "Category updated successfully.");
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Fail($"Error updating category: {ex.Message}");
            }
        }

        public async Task<ServiceResult<bool>> DeleteCategoryAsync(short id)
        {
            try
            {
                var category = await _categoryRepository.GetAllAsQueryable()
                    .Include(c => c.NewsArticles)
                    .FirstOrDefaultAsync(c => c.CategoryId == id);

                if (category == null)
                    return ServiceResult<bool>.Fail("Category not found.");

                if (category.NewsArticles.Any())
                {
                    return ServiceResult<bool>.Fail("Cannot delete category because it is associated with news articles.");
                }

                _categoryRepository.Remove(category);
                await _unitOfWork.SaveChangesAsync();
                return ServiceResult<bool>.Ok(true, "Category deleted successfully.");
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Fail($"Error deleting category: {ex.Message}");
            }
        }
    }
}
