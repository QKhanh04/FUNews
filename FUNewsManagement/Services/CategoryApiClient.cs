using Common;
using DataAccessObjects;
using FUNewsManagement.Services;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using ViewModel.Category;

namespace FUNewsManagement.Services
{
    public class CategoryApiClient : ICategoryService
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CategoryApiClient(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
        }



        public async Task<IEnumerable<Category>> GetAllActiveCategories()
        {

            var response = await _httpClient.GetFromJsonAsync<ODataResponse<Category>>("odata/Categories?$filter=IsActive eq true");
            return response?.Value ?? new List<Category>();
        }

        public async Task<IEnumerable<Category>> GetAllCategoriesAsync()
        {

            var response = await _httpClient.GetFromJsonAsync<ODataResponse<Category>>("odata/Categories");
            return response?.Value ?? new List<Category>();
        }

        public async Task<CategoryManagementViewModel> GetCategoryManagementAsync(string? search, int pageNumber, int pageSize)
        {

            var skip = (pageNumber - 1) * pageSize;
            var filter = string.IsNullOrEmpty(search) ? "" : $"$filter=contains(CategoryName, '{search}') or contains(CategoryDesciption, '{search}')&";
            
            var url = $"odata/Categories?{filter}$count=true&$top={pageSize}&$skip={skip}&$expand=ParentCategory,NewsArticles";
            var response = await _httpClient.GetFromJsonAsync<ODataResponse<Category>>(url);

            var items = new List<CategoryItemViewModel>();
            if (response?.Value != null)
            {
                foreach (var c in response.Value)
                {
                    items.Add(new CategoryItemViewModel
                    {
                        CategoryId = c.CategoryId,
                        CategoryName = c.CategoryName,
                        CategoryDesciption = c.CategoryDesciption,
                        ParentCategoryId = c.ParentCategoryId,
                        ParentCategoryName = c.ParentCategory?.CategoryName ?? "None",
                        IsActive = c.IsActive ?? false,
                        NewsCount = c.NewsArticles?.Count ?? 0
                    });
                }
            }

            // Fetch all active categories to populate drop downs, stats, etc.
            var allResponse = await _httpClient.GetFromJsonAsync<ODataResponse<Category>>("odata/Categories?$expand=NewsArticles");
            var allCats = allResponse?.Value ?? new List<Category>();

            var stats = new CategoryStatsViewModel
            {
                TotalCount = allCats.Count,
                ActiveCount = allCats.Count(c => c.IsActive == true),
                UnusedCount = allCats.Count(c => c.NewsArticles == null || !c.NewsArticles.Any()),
                GrowthThisMonth = 0
            };

            return new CategoryManagementViewModel
            {
                Categories = items,
                Stats = stats,
                TotalCount = response?.Count ?? 0,
                PageNumber = pageNumber,
                PageSize = pageSize,
                SearchTerm = search,
                AllCategories = allCats.Select(c => new CategoryItemViewModel
                {
                    CategoryId = c.CategoryId,
                    CategoryName = c.CategoryName
                }).ToList()
            };
        }

        public async Task<List<CategoryNodeViewModel>> GetCategoryTreeAsync()
        {
            var activeCategories = await GetAllActiveCategories();
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

            try
            {
                return await _httpClient.GetFromJsonAsync<Category>($"odata/Categories({id})");
            }
            catch
            {
                return null;
            }
        }

        public async Task<ServiceResult<bool>> AddCategoryAsync(Category category)
        {

            
            // Clear navigation properties to avoid EF insert issues
            category.ParentCategory = null;
            category.NewsArticles = null;

            var response = await _httpClient.PostAsJsonAsync("odata/Categories", category);
            if (response.IsSuccessStatusCode)
            {
                return ServiceResult<bool>.Ok(true);
            }
            var error = await response.Content.ReadAsStringAsync();
            return ServiceResult<bool>.Fail(error);
        }

        public async Task<ServiceResult<bool>> UpdateCategoryAsync(Category category)
        {

            
            // Clear navigation properties
            category.ParentCategory = null;
            category.NewsArticles = null;

            var response = await _httpClient.PutAsJsonAsync($"odata/Categories({category.CategoryId})", category);
            if (response.IsSuccessStatusCode)
            {
                return ServiceResult<bool>.Ok(true);
            }
            var error = await response.Content.ReadAsStringAsync();
            return ServiceResult<bool>.Fail(error);
        }

        public async Task<ServiceResult<bool>> DeleteCategoryAsync(short id)
        {

            var response = await _httpClient.DeleteAsync($"odata/Categories({id})");
            if (response.IsSuccessStatusCode)
            {
                return ServiceResult<bool>.Ok(true);
            }
            var error = await response.Content.ReadAsStringAsync();
            return ServiceResult<bool>.Fail(error);
        }
    }
}
