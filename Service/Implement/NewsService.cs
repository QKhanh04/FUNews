using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataAccessObjects;
using Repository.Interface;
using Service.Interface;
using Common;
using ViewModel.Home;
using Microsoft.EntityFrameworkCore;
using ViewModel.News;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Service.Hubs;

namespace Service.Implement
{
    public class NewsService : INewsService
    {
        private readonly INewsRepository _newsRepository;
        private readonly ITagRepository _tagRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHubContext<NewsHub> _hubContext;

        public NewsService(
            INewsRepository newsRepository, 
            ITagRepository tagRepository,
            ICategoryRepository categoryRepository,
            ICloudinaryService cloudinaryService,
            IUnitOfWork unitOfWork,
            IHubContext<NewsHub> hubContext)
        {
            _newsRepository = newsRepository;
            _tagRepository = tagRepository;
            _categoryRepository = categoryRepository;
            _cloudinaryService = cloudinaryService;
            _unitOfWork = unitOfWork;
            _hubContext = hubContext;
        }

        // ── Raw helper ──────────────────────────────────────────────────────
        public async Task<IEnumerable<NewsArticle>> GetAllNewsActive()
        {
            var news = await _newsRepository.GetAllNewsAsync();
            return news.Where(n => n.NewsStatus == true);
        }

        // ── Home page logic (all filtering / mapping lives here) ────────────
        public async Task<CursorResult<NewsCardViewModel>> GetForHomeAsync(
    string? search,
    short? categoryId,
    int? tagId,
    int? cursor,
    int pageSize)
        {
            IQueryable<NewsArticle> query = _newsRepository
                .GetActiveQueryable()
                .Include(n => n.Category)
                .OrderByDescending(n => n.NewsArticleId);

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(n =>
                    (n.NewsTitle != null && n.NewsTitle.Contains(search)) ||
                    (n.Headline != null && n.Headline.Contains(search)));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(n => n.CategoryId == categoryId.Value);
            }

            if (tagId.HasValue)
            {
                query = query.Where(n => n.Tags.Any(t => t.TagId == tagId.Value));
            }

            if (cursor.HasValue)
            {
                query = query.Where(n => n.NewsArticleId < cursor.Value);
            }

            var items = await query
                .Take(pageSize)
                .Select(n => MapToCard(n))
                .ToListAsync();

            return new CursorResult<NewsCardViewModel>
            {
                Items = items,
                NextCursor = items.LastOrDefault()?.NewsArticleId,
                HasMore = items.Count == pageSize
            };
        }

        // ── History page logic ──────────────────────────────────────────────
        public async Task<NewsManagementViewModel> GetNewsManagementAsync(
            short userId,
            string role,
            string? search,
            short? categoryId,
            bool? status,
            int pageNumber,
            int pageSize)
        {
            IQueryable<NewsArticle> query = _newsRepository.GetAllAsQueryable()
                .Include(n => n.Category)
                .Include(n => n.CreatedBy);

            // Access Control: Admin (0) sees all, Staff (1) sees their own
            if (role == "1")
            {
                query = query.Where(n => n.CreatedById == userId);
            }

            // Filters
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(n =>
                    (n.NewsTitle != null && n.NewsTitle.Contains(search)) ||
                    (n.Headline != null && n.Headline.Contains(search)));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(n => n.CategoryId == categoryId.Value);
            }

            if (status.HasValue)
            {
                query = query.Where(n => n.NewsStatus == status.Value);
            }

            // Stats
            var allArticlesQuery = _newsRepository.GetAllAsQueryable();
            if (role == "1") allArticlesQuery = allArticlesQuery.Where(n => n.CreatedById == userId);
            
            var allArticles = await allArticlesQuery.ToListAsync();
            var totalCount = allArticles.Count;
            var activeCount = allArticles.Count(n => n.NewsStatus == true);
            var draftCount = allArticles.Count(n => n.NewsStatus != true);

            var itemsCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(n => n.CreatedDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(n => new NewsManagementItemViewModel
                {
                    NewsArticleId = n.NewsArticleId,
                    NewsTitle = n.NewsTitle ?? string.Empty,
                    Headline = n.Headline,
                    CategoryName = n.Category != null ? n.Category.CategoryName : null,
                    CategoryId = n.CategoryId,
                    NewsStatus = n.NewsStatus,
                    CreatedDate = n.CreatedDate,
                    CreatedByName = n.CreatedBy != null ? n.CreatedBy.AccountName : "System",
                    ThumbnailUrl = n.ThumbnailUrl,
                    NewsContent = n.NewsContent,
                    SelectedTagIds = n.Tags.Select(t => t.TagId).ToList()
                })
                .ToListAsync();

            return new NewsManagementViewModel
            {
                Articles = items,
                Stats = new NewsManagementStatsViewModel
                {
                    TotalArticles = totalCount,
                    ActiveArticles = activeCount,
                    DraftArticles = draftCount,
                    GrowthRate = 0
                },
                TotalCount = itemsCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                SearchTerm = search,
                CategoryId = categoryId,
                Status = status
            };
        }

        public async Task<ServiceResult<bool>> AddNewsAsync(NewsArticle article, List<int> selectedTagIds, string? newTags, IFormFile? imageFile, string? userRole)
        {
            try
            {
                // Force ID to 0 for new records to ensure identity insert works
                article.NewsArticleId = 0;
                
                // Ensure navigation properties are null to prevent EF from trying to insert existing records as new
                article.Category = null;
                article.CreatedBy = null;

                // Handle Image
                if (imageFile != null)
                {
                    var uploadedUrl = await _cloudinaryService.UploadImageAsync(imageFile);
                    if (!string.IsNullOrEmpty(uploadedUrl))
                    {
                        article.ThumbnailUrl = uploadedUrl;
                    }
                }
                else if (!string.IsNullOrEmpty(article.ThumbnailUrl) && !article.ThumbnailUrl.Contains("cloudinary.com"))
                {
                    // Pasted external URL -> upload to Cloudinary
                    var cloudUrl = await _cloudinaryService.UploadImageFromUrlAsync(article.ThumbnailUrl);
                    if (!string.IsNullOrEmpty(cloudUrl))
                    {
                        article.ThumbnailUrl = cloudUrl;
                    }
                }

                article.CreatedDate = DateTime.UtcNow;
                article.ModifiedDate = DateTime.UtcNow;

                // Handle Tags
                article.Tags = new List<Tag>();
                
                // 1. Process Selected Tags
                if (selectedTagIds != null && selectedTagIds.Any())
                {
                    var selectedTags = await _tagRepository.GetAllAsQueryable()
                        .Where(t => selectedTagIds.Contains(t.TagId))
                        .ToListAsync();
                    
                    foreach (var tag in selectedTags)
                    {
                        article.Tags.Add(tag);
                    }
                }

                // 2. Process New Tags
                if (!string.IsNullOrWhiteSpace(newTags))
                {
                    var tagNames = newTags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    foreach (var name in tagNames)
                    {
                        var existingTag = await _tagRepository.GetAllAsQueryable()
                            .FirstOrDefaultAsync(t => t.TagName.ToLower() == name.ToLower());
                        
                        if (existingTag != null)
                        {
                            if (!article.Tags.Any(t => t.TagId == existingTag.TagId))
                                article.Tags.Add(existingTag);
                        }
                        else
                        {
                            var newTag = new Tag { TagName = name };
                            await _tagRepository.AddAsync(newTag);
                            article.Tags.Add(newTag);
                        }
                    }
                }

                // If IDs are 0 (Admin virtual account), set to null to avoid FK conflict
                if (article.CreatedById == 0) article.CreatedById = null;
                if (article.UpdatedById == 0) article.UpdatedById = null;

                // Approval Workflow enforce: If user is Staff (Role "1"), force status to false (Pending)
                if (userRole == "1")
                {
                    article.NewsStatus = false;
                }

                await _newsRepository.AddAsync(article);
                await _unitOfWork.SaveChangesAsync();
                await _hubContext.Clients.All.SendAsync("UpdateNews", "Create", userRole ?? "System", article.NewsTitle);
                return ServiceResult<bool>.Ok(true, "News article created successfully.");
            }
            catch (Exception ex)
            {
                var fullMessage = ex.Message + (ex.InnerException != null ? " | " + ex.InnerException.Message : "");
                Console.WriteLine($"[AddNewsAsync Error] {fullMessage}");
                return ServiceResult<bool>.Fail("Database Error: " + fullMessage);
            }
        }

        public async Task<ServiceResult<bool>> UpdateNewsAsync(NewsArticle article, List<int> selectedTagIds, string? newTags, IFormFile? imageFile, string? userRole)
        {
            try
            {
                var existing = await _newsRepository.GetAllAsQueryable()
                    .Include(n => n.Tags)
                    .FirstOrDefaultAsync(n => n.NewsArticleId == article.NewsArticleId);

                if (existing == null) return ServiceResult<bool>.Fail("Article not found.");

                // Handle Image
                if (imageFile != null)
                {
                    var uploadedUrl = await _cloudinaryService.UploadImageAsync(imageFile);
                    if (!string.IsNullOrEmpty(uploadedUrl))
                    {
                        existing.ThumbnailUrl = uploadedUrl;
                    }
                }
                else if (!string.IsNullOrEmpty(article.ThumbnailUrl))
                {
                    if (article.ThumbnailUrl != existing.ThumbnailUrl && !article.ThumbnailUrl.Contains("cloudinary.com"))
                    {
                        var cloudUrl = await _cloudinaryService.UploadImageFromUrlAsync(article.ThumbnailUrl);
                        existing.ThumbnailUrl = string.IsNullOrEmpty(cloudUrl) ? article.ThumbnailUrl : cloudUrl;
                    }
                    else
                    {
                        existing.ThumbnailUrl = article.ThumbnailUrl;
                    }
                }

                existing.NewsTitle = article.NewsTitle;
                existing.Headline = article.Headline;
                existing.NewsContent = article.NewsContent;
                existing.CategoryId = article.CategoryId;
                existing.ModifiedDate = DateTime.UtcNow;
                
                // Approval Workflow enforce: If user is Staff (Role "1"), force status to false (Pending)
                // If it is already true from previous, meaning Admin published it then Staff edit it, it should go back to Pending
                if (userRole == "1")
                {
                    existing.NewsStatus = false;
                }
                else
                {
                    existing.NewsStatus = article.NewsStatus;
                }
                
                // Track who updated it
                existing.UpdatedById = article.UpdatedById == 0 ? null : article.UpdatedById;

                // Handle Tags
                existing.Tags.Clear();
                
                // 1. Selected Tags
                if (selectedTagIds != null && selectedTagIds.Any())
                {
                    var allTags = await _tagRepository.GetAllAsync();
                    var tagsToAdd = allTags.Where(t => selectedTagIds.Contains(t.TagId)).ToList();
                    foreach (var tag in tagsToAdd)
                    {
                        existing.Tags.Add(tag);
                    }
                }

                // 2. New Tags
                if (!string.IsNullOrWhiteSpace(newTags))
                {
                    var tagNames = newTags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    foreach (var name in tagNames)
                    {
                        var existingTag = await _tagRepository.GetAllAsQueryable()
                            .FirstOrDefaultAsync(t => t.TagName.ToLower() == name.ToLower());
                        
                        if (existingTag != null)
                        {
                            if (!existing.Tags.Any(t => t.TagId == existingTag.TagId))
                                existing.Tags.Add(existingTag);
                        }
                        else
                        {
                            var newTag = new Tag { TagName = name };
                            await _tagRepository.AddAsync(newTag);
                            existing.Tags.Add(newTag);
                        }
                    }
                }

                _newsRepository.Update(existing);
                await _unitOfWork.SaveChangesAsync();
                await _hubContext.Clients.All.SendAsync("UpdateNews", "Update", userRole ?? "System", article.NewsTitle);
                return ServiceResult<bool>.Ok(true, "News article updated successfully.");
            }
            catch (Exception ex)
            {
                var fullMessage = ex.Message + (ex.InnerException != null ? " | " + ex.InnerException.Message : "");
                Console.WriteLine($"[UpdateNewsAsync Error] {fullMessage}");
                return ServiceResult<bool>.Fail("Update Error: " + fullMessage);
            }
        }

        public async Task<ServiceResult<bool>> DeleteNewsAsync(int id)
        {
            try
            {
                var existing = await _newsRepository.GetAllAsQueryable()
                    .Include(n => n.Tags)
                    .FirstOrDefaultAsync(n => n.NewsArticleId == id);
                
                if (existing == null) return ServiceResult<bool>.Fail("Article not found.");

                // Clear tags first to avoid FK conflict in NewsTag join table
                existing.Tags.Clear();
                _newsRepository.Update(existing);
                await _unitOfWork.SaveChangesAsync();

                _newsRepository.Remove(existing);
                await _unitOfWork.SaveChangesAsync();
                await _hubContext.Clients.All.SendAsync("UpdateNews", "Delete", "System", existing.NewsTitle);
                return ServiceResult<bool>.Ok(true, "News article deleted successfully.");
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Fail("Delete Error: " + ex.Message);
            }
        }

        public async Task<ServiceResult<bool>> ApproveNewsAsync(int id)
        {
            try
            {
                var existing = await _newsRepository.GetByIdAsync(id);
                
                if (existing == null) return ServiceResult<bool>.Fail("Article not found.");

                existing.NewsStatus = true;
                _newsRepository.Update(existing);
                await _unitOfWork.SaveChangesAsync();
                
                await _hubContext.Clients.All.SendAsync("UpdateNews", "Approve", "System", existing.NewsTitle);
                return ServiceResult<bool>.Ok(true, "News article approved successfully.");
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Fail("Approve Error: " + ex.Message);
            }
        }

        // ── Private mapper ──────────────────────────────────────────────────
        private static NewsCardViewModel MapToCard(NewsArticle n) => new()
        {
            NewsArticleId = n.NewsArticleId,
            NewsTitle = n.NewsTitle ?? string.Empty,
            Headline = n.Headline,
            NewsContent = n.NewsContent,
            CreatedDate = n.CreatedDate,
            CategoryName = n.Category?.CategoryName,
            ThumbnailUrl = n.ThumbnailUrl
        };

        public async Task<NewsHistoryViewModel> GetHistoryAsync(
            short userId,
            string? search,
            short? categoryId,
            DateTime? startDate,
            DateTime? endDate,
            int pageNumber,
            int pageSize)
        {
            var query = _newsRepository.GetAllAsQueryable()
                .Include(n => n.Category)
                .Where(n => n.CreatedById == userId);

            // Filters
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(n =>
                    (n.NewsTitle != null && n.NewsTitle.Contains(search)) ||
                    (n.Headline != null && n.Headline.Contains(search)));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(n => n.CategoryId == categoryId.Value);
            }

            if (startDate.HasValue)
            {
                query = query.Where(n => n.CreatedDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(n => n.CreatedDate <= endDate.Value);
            }

            // Stats (Based on all user's articles, not just filtered ones)
            var allUserArticles = await _newsRepository.GetAllAsQueryable()
                .Include(n => n.Category)
                .Where(n => n.CreatedById == userId)
                .ToListAsync();

            var totalCreated = allUserArticles.Count;
            var mostFreqCat = allUserArticles
                .GroupBy(n => n.Category?.CategoryName)
                .Where(g => g.Key != null)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault();


            var stats = new NewsHistoryStatsViewModel
            {
                TotalCreated = totalCreated,
                MostFrequentCategory = mostFreqCat?.Key,
                MostFrequentCategoryCount = mostFreqCat?.Count() ?? 0,
                GrowthRate = 0,
                TotalViews = "0" // Placeholder
            };

            // Pagination
            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(n => n.CreatedDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(n => new NewsHistoryItemViewModel
                {
                    NewsArticleId = n.NewsArticleId,
                    NewsTitle = n.NewsTitle ?? string.Empty,
                    CategoryName = n.Category != null ? n.Category.CategoryName : null,
                    NewsStatus = n.NewsStatus,
                    CreatedDate = n.CreatedDate,
                    ThumbnailUrl = n.ThumbnailUrl
                })
                .ToListAsync();

            return new NewsHistoryViewModel
            {
                Articles = items,
                Stats = stats,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                SearchTerm = search,
                CategoryId = categoryId,
                StartDate = startDate,
                EndDate = endDate
            };
        }

        public async Task<NewsReportViewModel> GetReportAsync(
            DateTime? startDate,
            DateTime? endDate,
            int pageNumber,
            int pageSize)
        {
            var query = _newsRepository.GetAllAsQueryable()
                .Include(n => n.Category)
                .Include(n => n.CreatedBy)
                .AsQueryable();

            // Date Filters
            if (startDate.HasValue)
            {
                var start = startDate.Value.Date;
                query = query.Where(n => n.CreatedDate >= start);
            }

            if (endDate.HasValue)
            {
                var end = endDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(n => n.CreatedDate <= end);
            }

            // Descending Sort by CreatedDate
            query = query.OrderByDescending(n => n.CreatedDate);

            // Calculate Statistics for the filtered set
            var filteredArticles = await query.ToListAsync();
            
            var totalArticles = filteredArticles.Count;
            var activeArticles = filteredArticles.Count(n => n.NewsStatus == true);
            var draftArticles = totalArticles - activeArticles;

            var topCatGroup = filteredArticles
                .GroupBy(n => n.Category?.CategoryName)
                .Where(g => g.Key != null)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault();

            var stats = new NewsReportStatsViewModel
            {
                TotalArticles = totalArticles,
                ActiveArticles = activeArticles,
                DraftArticles = draftArticles,
                TopCategory = topCatGroup?.Key,
                TopCategoryCount = topCatGroup?.Count() ?? 0
            };

            // Pagination
            var totalCount = totalArticles;
            var items = filteredArticles
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(n => new NewsReportItemViewModel
                {
                    NewsArticleId = n.NewsArticleId,
                    NewsTitle = n.NewsTitle ?? string.Empty,
                    CategoryName = n.Category?.CategoryName,
                    AuthorName = n.CreatedBy?.AccountName ?? "System",
                    NewsStatus = n.NewsStatus,
                    CreatedDate = n.CreatedDate,
                    ThumbnailUrl = n.ThumbnailUrl
                })
                .ToList();

            return new NewsReportViewModel
            {
                Articles = items,
                Stats = stats,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                StartDate = startDate,
                EndDate = endDate
            };
        }
    }
}
