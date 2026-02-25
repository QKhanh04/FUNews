using System.Collections.Generic;
using Common;

namespace ViewModel.Home
{
    public class HomeIndexViewModel
    {
        public NewsCardViewModel? FeaturedArticle { get; set; }

        public CursorResult<NewsCardViewModel> Grid { get; set; }
            = new CursorResult<NewsCardViewModel>();

        public IEnumerable<DataAccessObjects.Category> Categories { get; set; } = new List<DataAccessObjects.Category>();
        public short? SelectedCategoryId { get; set; }
        public int? SelectedTagId { get; set; }
        public string? SearchKeyword { get; set; }
    }
}
