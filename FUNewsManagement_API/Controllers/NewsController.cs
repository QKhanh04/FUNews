using DataAccessObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Results;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Repository.Interface;
using Service.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ViewModel.News;

namespace FUNewsManagement_API.Controllers
{
    public class NewsController : ODataController
    {
        private readonly INewsRepository _newsRepository;
        private readonly INewsService _newsService;

        public NewsController(INewsRepository newsRepository, INewsService newsService)
        {
            _newsRepository = newsRepository;
            _newsService = newsService;
        }

        // OData Default Routes
        [EnableQuery]
        public IActionResult Get()
        {
            return Ok(_newsRepository.GetAllAsQueryable());
        }

        [EnableQuery]
        public IActionResult Get([FromRoute] int key)
        {
            var query = _newsRepository.GetAllAsQueryable().Where(n => n.NewsArticleId == key);
            return Ok(SingleResult.Create(query));
        }

        // Custom API Routes
        [HttpGet("api/News/management")]
        [Authorize]
        public async Task<IActionResult> GetNewsManagement(
            [FromQuery] short userId,
            [FromQuery] string role,
            [FromQuery] string? search,
            [FromQuery] short? categoryId,
            [FromQuery] bool? status,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _newsService.GetNewsManagementAsync(userId, role, search, categoryId, status, pageNumber, pageSize);
            return Ok(result);
        }

        [HttpGet("api/News/history")]
        [Authorize]
        public async Task<IActionResult> GetHistory(
            [FromQuery] short userId,
            [FromQuery] string? search,
            [FromQuery] short? categoryId,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _newsService.GetHistoryAsync(userId, search, categoryId, startDate, endDate, pageNumber, pageSize);
            return Ok(result);
        }

        [HttpGet("api/News/report")]
        [Authorize(Roles = "0")]
        public async Task<IActionResult> GetReport(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _newsService.GetReportAsync(startDate, endDate, pageNumber, pageSize);
            return Ok(result);
        }

        [HttpPost("api/News")]
        [Authorize(Roles = "1")]
        public async Task<IActionResult> PostApi([FromBody] CreateNewsRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userRole = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
            var result = await _newsService.AddNewsAsync(request.Article, request.SelectedTagIds, request.NewTags, null, userRole);
            
            if (result.IsSuccess) return Ok(request.Article);
            return BadRequest(result.Message);
        }

        [HttpPut("api/News/{id}")]
        [Authorize(Roles = "1")]
        public async Task<IActionResult> PutApi([FromRoute] int id, [FromBody] UpdateNewsRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (id != request.Article.NewsArticleId) return BadRequest();

            var userRole = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
            var result = await _newsService.UpdateNewsAsync(request.Article, request.SelectedTagIds, request.NewTags, null, userRole);

            if (result.IsSuccess) return Ok(request.Article);
            return BadRequest(result.Message);
        }

        [HttpDelete("api/News/{id}")]
        [Authorize(Roles = "1")]
        public async Task<IActionResult> DeleteApi([FromRoute] int id)
        {
            var result = await _newsService.DeleteNewsAsync(id);
            if (result.IsSuccess) return NoContent();
            return BadRequest(result.Message);
        }

        [HttpPut("api/News/{id}/approve")]
        [Authorize(Roles = "0")]
        public async Task<IActionResult> ApproveApi([FromRoute] int id)
        {
            var result = await _newsService.ApproveNewsAsync(id);
            if (result.IsSuccess) return NoContent();
            return BadRequest(result.Message);
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
