namespace ViewModel.Home
{
    public class CategoryFilterViewModel
    {
        public short CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}
