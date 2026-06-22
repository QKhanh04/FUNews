using System.Collections.Generic;

namespace ViewModel.Category
{
    public class CategoryNodeViewModel
    {
        public short CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string CategoryDescription { get; set; } = string.Empty;
        public List<CategoryNodeViewModel> SubCategories { get; set; } = new();
    }
}
