using System;
using System.Collections.Generic;

namespace ViewModel.Home
{
    public class NewsDetailViewModel
    {
        public int NewsArticleId { get; set; }
        public string NewsTitle { get; set; } = string.Empty;
        public string Headline { get; set; } = string.Empty;
        public string? NewsContent { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string? CategoryName { get; set; }
        public short? CategoryId { get; set; }
        public string? ThumbnailUrl { get; set; }
        public string? CreatedByName { get; set; }
        public string? CreatedByRole { get; set; }
        public Dictionary<int, string> Tags { get; set; } = new();
        public List<NewsCardViewModel> RelatedArticles { get; set; } = new();

        public int ReadingMinutes =>
            string.IsNullOrWhiteSpace(NewsContent)
                ? 1
                : Math.Max(1, NewsContent.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length / 200);
    }
}