using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using FUNewsManagement.Services;
using ViewModel.News;
using System.Security.Claims;

namespace FUNewsManagement.Pages.News
{
    [Authorize(Roles = "1,0")] // Staff = 1, Admin = 0
    public class DashboardModel : PageModel
    {
        private readonly INewsService _newsService;

        public DashboardModel(INewsService newsService)
        {
            _newsService = newsService;
        }

        public NewsHistoryViewModel HistoryData { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!short.TryParse(userIdStr, out short userId))
                return RedirectToPage("/Account/Login");

            // Reusing history stats logic for dashboard summary
            HistoryData = await _newsService.GetHistoryAsync(
                userId, null, null, null, null, 1, 1);
            
            return Page();
        }
    }
}
