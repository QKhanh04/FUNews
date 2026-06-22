using System.Collections.Generic;
using System.Threading.Tasks;
using DataAccessObjects;
using Common;
using ViewModel.Home;
using Microsoft.AspNetCore.Http;

namespace FUNewsManagement.Services
{
    public interface INewsService
    {
        Task<IEnumerable<NewsArticle>> GetAllNewsActive();
        Task<NewsArticle?> GetNewsByIdAsync(int id);

        Task<CursorResult<NewsCardViewModel>> GetForHomeAsync(
            string? search,
            short? categoryId,
            int? tagId,
            int? cursor,
            int pageSize);

        Task<ViewModel.News.NewsHistoryViewModel> GetHistoryAsync(
            short userId,
            string? search,
            short? categoryId,
            DateTime? startDate,
            DateTime? endDate,
            int pageNumber,
            int pageSize);

        Task<ViewModel.News.NewsManagementViewModel> GetNewsManagementAsync(
            short userId,
            string role,
            string? search,
            short? categoryId,
            bool? status,
            int pageNumber,
            int pageSize);

        Task<ServiceResult<bool>> AddNewsAsync(NewsArticle article, List<int> selectedTagIds, string? newTags, IFormFile? imageFile, string? userRole);
        Task<ServiceResult<bool>> UpdateNewsAsync(NewsArticle article, List<int> selectedTagIds, string? newTags, IFormFile? imageFile, string? userRole);
        Task<ServiceResult<bool>> DeleteNewsAsync(int id);
        Task<ServiceResult<bool>> ApproveNewsAsync(int id);
        
        Task<ViewModel.News.NewsReportViewModel> GetReportAsync(
            DateTime? startDate,
            DateTime? endDate,
            int pageNumber,
            int pageSize);
    }
}

