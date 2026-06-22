using System.Collections.Generic;
using DataAccessObjects;

namespace ViewModel.Category
{
    public class CategoryManagementViewModel
    {
        public List<CategoryItemViewModel> Categories { get; set; } = new();
        public CategoryStatsViewModel Stats { get; set; } = new();
        
        // Pagination & Filters
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalCount { get; set; }
        public string? SearchTerm { get; set; }
        public List<CategoryItemViewModel> AllCategories { get; set; } = new();
    }

    public class CategoryItemViewModel
    {
        public short CategoryId { get; set; }
        public string CategoryName { get; set; } = null!;
        public string CategoryDesciption { get; set; } = null!;
        public short? ParentCategoryId { get; set; }
        public string? ParentCategoryName { get; set; }
        public bool IsActive { get; set; }
        public int NewsCount { get; set; }
    }

    public class CategoryStatsViewModel
    {
        public int TotalCount { get; set; }
        public int ActiveCount { get; set; }
        public int UnusedCount { get; set; }
        public int GrowthThisMonth { get; set; }
    }
}
