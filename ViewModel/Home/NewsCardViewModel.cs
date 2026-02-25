using System;

namespace ViewModel.Home
{
    public class NewsCardViewModel
    {
        public int NewsArticleId { get; set; }
        public string NewsTitle { get; set; } = string.Empty;
        public string Headline { get; set; } = string.Empty;
        public string? NewsContent { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string? CategoryName { get; set; }
        public string? ThumbnailUrl { get; set; }

        /// <summary>
        /// Estimated reading time in minutes (rough: 200 words/min)
        /// </summary>
        public int ReadingMinutes =>
            string.IsNullOrWhiteSpace(NewsContent)
                ? 1
                : Math.Max(1, NewsContent.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length / 200);
    }
}
