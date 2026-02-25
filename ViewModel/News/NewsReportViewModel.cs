using System;
using System.Collections.Generic;

namespace ViewModel.News
{
    public class NewsReportViewModel
    {
        public List<NewsReportItemViewModel> Articles { get; set; } = new();
        public NewsReportStatsViewModel Stats { get; set; } = new();
        
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class NewsReportItemViewModel
    {
        public int NewsArticleId { get; set; }
        public string NewsTitle { get; set; } = string.Empty;
        public string? CategoryName { get; set; }
        public string? AuthorName { get; set; }
        public bool? NewsStatus { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string? ThumbnailUrl { get; set; }
    }

    public class NewsReportStatsViewModel
    {
        public int TotalArticles { get; set; }
        public int ActiveArticles { get; set; }
        public int DraftArticles { get; set; }
        public string? TopCategory { get; set; }
        public int TopCategoryCount { get; set; }
    }
}
