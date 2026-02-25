using System;
using System.Collections.Generic;

namespace ViewModel.News
{
    public class NewsManagementViewModel
    {
        public List<NewsManagementItemViewModel> Articles { get; set; } = new();
        public NewsManagementStatsViewModel Stats { get; set; } = new();
        
        // Pagination & Filters
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalCount { get; set; }
        public string? SearchTerm { get; set; }
        public short? CategoryId { get; set; }
        public bool? Status { get; set; }
    }

    public class NewsManagementItemViewModel
    {
        public int NewsArticleId { get; set; }
        public string NewsTitle { get; set; } = null!;
        public string? Headline { get; set; }
        public string? CategoryName { get; set; }
        public short? CategoryId { get; set; }
        public bool? NewsStatus { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string? CreatedByName { get; set; }
        public string? ThumbnailUrl { get; set; }
        public string? NewsContent { get; set; }
        public List<int> SelectedTagIds { get; set; } = new();
    }

    public class NewsManagementStatsViewModel
    {
        public int TotalArticles { get; set; }
        public int ActiveArticles { get; set; }
        public int DraftArticles { get; set; }
        public double GrowthRate { get; set; }
    }
}
