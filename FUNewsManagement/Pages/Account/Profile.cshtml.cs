using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using FUNewsManagement.Services;
using System.Security.Claims;
using ViewModel.Account;
using DataAccessObjects;

namespace FUNewsManagement.Pages.Account
{
    [Microsoft.AspNetCore.Authorization.Authorize]
    public class ProfileModel : PageModel
    {
        private readonly IAccountService _accountService;

        public ProfileModel(IAccountService accountService)
        {
            _accountService = accountService;
        }

        [BindProperty]
        public ProfileViewModel ProfileVM { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!short.TryParse(userIdStr, out short userId))
                return RedirectToPage("/Account/Login");

            var result = await _accountService.GetAccountById(userId);
            if (!result.IsSuccess)
            {
                TempData["ToastMessage"] = result.Message;
                TempData["ToastType"] = "danger";
                return RedirectToPage("/Index");
            }

            var account = result.Data!;
            ProfileVM = new ProfileViewModel
            {
                AccountId    = account.AccountId,
                AccountName  = account.AccountName,
                AccountEmail = account.AccountEmail,
                AccountRole  = account.AccountRole,
                IsGoogleAccount = account.IsGoogleAccount ?? false,
                GoogleId     = account.GoogleId,
                CreatedAt    = account.CreatedAt
            };

            return Page();
        }

        public async Task<IActionResult> OnPostUpdateProfileAsync()
        {
            if (!ModelState.IsValid)
            {
                await RePopulateProfileModel();
                return Page();
            }

            var account = new SystemAccount
            {
                AccountId = ProfileVM.AccountId,
                AccountName = ProfileVM.AccountName
            };

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            short.TryParse(userIdStr, out short currentUserId);

            var result = await _accountService.UpdateAccount(account, currentUserId);
            if (!result.IsSuccess)
            {
                TempData["ToastMessage"] = result.Message;
                TempData["ToastType"] = "danger";
                await RePopulateProfileModel();
                return Page();
            }

            TempData["ToastMessage"] = result.Message;
            TempData["ToastType"] = "success";

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostChangePasswordAsync()
        {
            if (string.IsNullOrEmpty(ProfileVM.CurrentPassword) || string.IsNullOrEmpty(ProfileVM.NewPassword) || string.IsNullOrEmpty(ProfileVM.ConfirmPassword))
            {
                TempData["ToastMessage"] = "Please fill in all password fields.";
                TempData["ToastType"] = "warning";
                await RePopulateProfileModel();
                return Page();
            }

            if (ProfileVM.NewPassword != ProfileVM.ConfirmPassword)
            {
                ModelState.AddModelError("ProfileVM.ConfirmPassword", "New password and confirmation do not match.");
                await RePopulateProfileModel();
                return Page();
            }

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!short.TryParse(userIdStr, out short userId))
                return RedirectToPage("/Account/Login");

            var result = await _accountService.ChangePassword(userId, ProfileVM.CurrentPassword, ProfileVM.NewPassword);
            if (!result.IsSuccess)
            {
                TempData["ToastMessage"] = result.Message;
                TempData["ToastType"] = "danger";
                await RePopulateProfileModel();
                return Page();
            }

            TempData["ToastMessage"] = result.Message;
            TempData["ToastType"] = "success";

            return RedirectToPage();
        }

        private async Task RePopulateProfileModel()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (short.TryParse(userIdStr, out short userId))
            {
                var result = await _accountService.GetAccountById(userId);
                if (result.IsSuccess)
                {
                    var account = result.Data!;
                    ProfileVM.AccountId = account.AccountId;
                    ProfileVM.AccountName = account.AccountName;
                    ProfileVM.AccountEmail = account.AccountEmail;
                    ProfileVM.AccountRole = account.AccountRole;
                    ProfileVM.IsGoogleAccount = account.IsGoogleAccount ?? false;
                    ProfileVM.GoogleId = account.GoogleId;
                    ProfileVM.CreatedAt = account.CreatedAt;

                    ModelState.Remove("ProfileVM.AccountName");
                }
            }
        }
    }
}
