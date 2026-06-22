using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using FUNewsManagement.Services;
using ViewModel.Account;
using DataAccessObjects;
using System.Security.Claims;

namespace FUNewsManagement.Pages.Account
{
    [Authorize(Roles = "0")] // Admin only
    public class ManageModel : PageModel
    {
        private readonly IAccountService _accountService;

        public ManageModel(IAccountService accountService)
        {
            _accountService = accountService;
        }

        public AccountManagementViewModel ManagementData { get; set; } = default!;
        public string? CurrentUserId { get; set; }

        public async Task OnGetAsync(string? search, int pageNumber = 1, int pageSize = 10)
        {
            ManagementData = await _accountService.GetAccountManagementAsync(search, pageNumber, pageSize);
            CurrentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        public async Task<IActionResult> OnPostCreateAsync(SystemAccount account)
        {
            var result = await _accountService.AddAccount(account);
            TempData["ToastMessage"] = result.Message;
            TempData["ToastType"] = result.IsSuccess ? "success" : "danger";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEditAsync(SystemAccount account)
        {
            string? currentUserIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            short.TryParse(currentUserIdStr, out short currentUserId);

            var result = await _accountService.UpdateAccount(account, currentUserId);
            TempData["ToastMessage"] = result.Message;
            TempData["ToastType"] = result.IsSuccess ? "success" : "danger";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(short id)
        {
            var result = await _accountService.DeleteAccount(id);
            TempData["ToastMessage"] = result.Message;
            TempData["ToastType"] = result.IsSuccess ? "success" : "danger";
            return RedirectToPage();
        }
    }
}
