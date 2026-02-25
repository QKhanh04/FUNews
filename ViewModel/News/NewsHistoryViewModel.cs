using System;
using System.Collections.Generic;

namespace ViewModel.News
{
    public class NewsHistoryViewModel
    {
        public List<NewsHistoryItemViewModel> Articles { get; set; } = new();
        public NewsHistoryStatsViewModel Stats { get; set; } = new();
        
        // Pagination & Filters
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalCount { get; set; }
        public string? SearchTerm { get; set; }
        public short? CategoryId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class NewsHistoryItemViewModel
    {
        public int NewsArticleId { get; set; }
        public string NewsTitle { get; set; } = null!;
        public string? CategoryName { get; set; }
        public bool? NewsStatus { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string? ThumbnailUrl { get; set; }
    }

    public class NewsHistoryStatsViewModel
    {
        public int TotalCreated { get; set; }
        public string? MostFrequentCategory { get; set; }
        public int MostFrequentCategoryCount { get; set; }
        public double GrowthRate { get; set; } // Represented as percentage, e.g., 12 for +12%
        public string TotalViews { get; set; } = "0"; // Formatted string like "45.2K"
    }
}
