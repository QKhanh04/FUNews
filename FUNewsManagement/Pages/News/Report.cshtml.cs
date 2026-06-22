using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using FUNewsManagement.Services;
using ViewModel.News;

namespace FUNewsManagement.Pages.News
{
    [Authorize(Roles = "0")] // Admin only
    public class ReportModel : PageModel
    {
        private readonly INewsService _newsService;

        public ReportModel(INewsService newsService)
        {
            _newsService = newsService;
        }

        public NewsReportViewModel ReportData { get; set; } = default!;

        public async Task OnGetAsync(DateTime? startDate, DateTime? endDate, int pageNumber = 1, int pageSize = 10)
        {
            ReportData = await _newsService.GetReportAsync(startDate, endDate, pageNumber, pageSize);
        }
    }
}
