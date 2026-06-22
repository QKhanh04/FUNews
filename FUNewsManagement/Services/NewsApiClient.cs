using Common;
using DataAccessObjects;
using FUNewsManagement.Services;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using ViewModel.Home;
using ViewModel.News;

namespace FUNewsManagement.Services
{
    public class NewsApiClient : INewsService
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ICloudinaryService _cloudinaryService;

        public NewsApiClient(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, ICloudinaryService cloudinaryService)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
            _cloudinaryService = cloudinaryService;
        }



        public async Task<ServiceResult<bool>> AddNewsAsync(NewsArticle article, List<int> selectedTagIds, string? newTags, IFormFile? imageFile, string? userRole)
        {


            // 1. Upload image to Cloudinary from Frontend if file exists
            if (imageFile != null)
            {
                var uploadedUrl = await _cloudinaryService.UploadImageAsync(imageFile);
                if (!string.IsNullOrEmpty(uploadedUrl))
                {
                    article.ThumbnailUrl = uploadedUrl;
                }
            }

            var request = new CreateNewsRequest
            {
                Article = article,
                SelectedTagIds = selectedTagIds,
                NewTags = newTags
            };

            var response = await _httpClient.PostAsJsonAsync("api/News", request);
            if (response.IsSuccessStatusCode)
            {
                return ServiceResult<bool>.Ok(true);
            }

            var error = await response.Content.ReadAsStringAsync();
            return ServiceResult<bool>.Fail(error);
        }

        public async Task<ServiceResult<bool>> DeleteNewsAsync(int id)
        {

            var response = await _httpClient.DeleteAsync($"api/News/{id}");
            if (response.IsSuccessStatusCode)
            {
                return ServiceResult<bool>.Ok(true);
            }
            var error = await response.Content.ReadAsStringAsync();
            return ServiceResult<bool>.Fail(error);
        }

        public async Task<IEnumerable<NewsArticle>> GetAllNewsActive()
        {

            var response = await _httpClient.GetFromJsonAsync<ODataResponse<NewsArticle>>("odata/News?$filter=NewsStatus eq true");
            return response?.Value ?? new List<NewsArticle>();
        }

        public async Task<NewsArticle?> GetNewsByIdAsync(int id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"odata/News({id})?$expand=Category,CreatedBy,Tags");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<NewsArticle>();
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        public async Task<CursorResult<NewsCardViewModel>> GetForHomeAsync(string? search, short? categoryId, int? tagId, int? cursor, int pageSize)
        {
            var filters = new List<string> { "NewsStatus eq true" };
            if (!string.IsNullOrWhiteSpace(search))
            {
                filters.Add($"(contains(NewsTitle, '{search}') or contains(Headline, '{search}'))");
            }
            if (categoryId.HasValue)
            {
                filters.Add($"CategoryId eq {categoryId}");
            }
            if (tagId.HasValue)
            {
                filters.Add($"Tags/any(t: t/TagId eq {tagId})");
            }
            if (cursor.HasValue)
            {
                filters.Add($"NewsArticleId lt {cursor}");
            }

            var filterStr = string.Join(" and ", filters);
            var url = $"odata/News?$filter={filterStr}&$orderby=NewsArticleId desc&$top={pageSize}&$expand=Category";


            var response = await _httpClient.GetFromJsonAsync<ODataResponse<NewsArticle>>(url);
            var items = new List<NewsCardViewModel>();
            if (response?.Value != null)
            {
                foreach (var n in response.Value)
                {
                    items.Add(new NewsCardViewModel
                    {
                        NewsArticleId = n.NewsArticleId,
                        NewsTitle = n.NewsTitle ?? string.Empty,
                        Headline = n.Headline,
                        NewsContent = n.NewsContent,
                        CreatedDate = n.CreatedDate,
                        CategoryName = n.Category?.CategoryName,
                        ThumbnailUrl = n.ThumbnailUrl
                    });
                }
            }

            return new CursorResult<NewsCardViewModel>
            {
                Items = items,
                NextCursor = items.LastOrDefault()?.NewsArticleId,
                HasMore = items.Count == pageSize
            };
        }

        public async Task<NewsHistoryViewModel> GetHistoryAsync(short userId, string? search, short? categoryId, DateTime? startDate, DateTime? endDate, int pageNumber, int pageSize)
        {

            var startStr = startDate.HasValue ? startDate.Value.ToString("o") : "";
            var endStr = endDate.HasValue ? endDate.Value.ToString("o") : "";
            
            var url = $"api/News/history?userId={userId}&search={search}&categoryId={categoryId}&startDate={startStr}&endDate={endStr}&pageNumber={pageNumber}&pageSize={pageSize}";
            var result = await _httpClient.GetFromJsonAsync<NewsHistoryViewModel>(url);
            return result ?? new NewsHistoryViewModel();
        }

        public async Task<NewsManagementViewModel> GetNewsManagementAsync(short userId, string role, string? search, short? categoryId, bool? status, int pageNumber, int pageSize)
        {

            var url = $"api/News/management?userId={userId}&role={role}&search={search}&categoryId={categoryId}&status={status}&pageNumber={pageNumber}&pageSize={pageSize}";
            var result = await _httpClient.GetFromJsonAsync<NewsManagementViewModel>(url);
            return result ?? new NewsManagementViewModel();
        }

        public async Task<NewsReportViewModel> GetReportAsync(DateTime? startDate, DateTime? endDate, int pageNumber, int pageSize)
        {

            var startStr = startDate.HasValue ? startDate.Value.ToString("o") : "";
            var endStr = endDate.HasValue ? endDate.Value.ToString("o") : "";

            var url = $"api/News/report?startDate={startStr}&endDate={endStr}&pageNumber={pageNumber}&pageSize={pageSize}";
            var result = await _httpClient.GetFromJsonAsync<NewsReportViewModel>(url);
            return result ?? new NewsReportViewModel();
        }

        public async Task<ServiceResult<bool>> UpdateNewsAsync(NewsArticle article, List<int> selectedTagIds, string? newTags, IFormFile? imageFile, string? userRole)
        {


            // 1. Upload image to Cloudinary from Frontend if file exists
            if (imageFile != null)
            {
                var uploadedUrl = await _cloudinaryService.UploadImageAsync(imageFile);
                if (!string.IsNullOrEmpty(uploadedUrl))
                {
                    article.ThumbnailUrl = uploadedUrl;
                }
            }

            var request = new UpdateNewsRequest
            {
                Article = article,
                SelectedTagIds = selectedTagIds,
                NewTags = newTags
            };

            var response = await _httpClient.PutAsJsonAsync($"api/News/{article.NewsArticleId}", request);
            if (response.IsSuccessStatusCode)
            {
                return ServiceResult<bool>.Ok(true);
            }

            var error = await response.Content.ReadAsStringAsync();
            return ServiceResult<bool>.Fail(error);
        }

        public async Task<ServiceResult<bool>> ApproveNewsAsync(int id)
        {
            var response = await _httpClient.PutAsync($"api/News/{id}/approve", null);
            return response.IsSuccessStatusCode
                ? ServiceResult<bool>.Ok(true)
                : ServiceResult<bool>.Fail("Approve failed");
        }
    }

    public class CreateNewsRequest
    {
        public NewsArticle Article { get; set; } = null!;
        public List<int> SelectedTagIds { get; set; } = new();
        public string? NewTags { get; set; }
    }

    public class UpdateNewsRequest
    {
        public NewsArticle Article { get; set; } = null!;
        public List<int> SelectedTagIds { get; set; } = new();
        public string? NewTags { get; set; }
    }
}
