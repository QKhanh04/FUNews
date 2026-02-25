using System.ComponentModel.DataAnnotations;

namespace ViewModel.Account
{
    public class ProfileViewModel
    {
        public short AccountId { get; set; }

        [Required(ErrorMessage = "Account Name is required")]
        public string? AccountName { get; set; }

        public string? AccountEmail { get; set; }

        public int? AccountRole { get; set; }

        public bool IsGoogleAccount { get; set; }

        public string? GoogleId { get; set; }

        public DateTime? CreatedAt { get; set; }

        // Change Password fields
        [DataType(DataType.Password)]
        public string? CurrentPassword { get; set; }

        [DataType(DataType.Password)]
        [MinLength(8, ErrorMessage = "New password must be at least 8 characters long")]
        public string? NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "The password and confirmation password do not match.")]
        public string? ConfirmPassword { get; set; }
    }
}
