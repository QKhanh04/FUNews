using Common;
using DataAccessObjects;
using FUNewsManagement.Services;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace FUNewsManagement.Services
{
    public class TagApiClient : ITagService
    {
        private readonly HttpClient _httpClient;

        // Constructor no longer needs IHttpContextAccessor because AuthHeaderHandler handles tokens!
        public TagApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<IEnumerable<Tag>> GetTagsAsync()
        {
            var response = await _httpClient.GetFromJsonAsync<ODataResponse<Tag>>("odata/Tags");
            return response?.Value ?? new List<Tag>();
        }

        public async Task<List<Tag>> GetAllAsync()
        {
            var response = await _httpClient.GetFromJsonAsync<ODataResponse<Tag>>("odata/Tags");
            return response?.Value ?? new List<Tag>();
        }

        public async Task<ServiceResult<bool>> AddTagAsync(Tag tag)
        {
            // Simple placeholder for add tag if needed, otherwise just throw
            var response = await _httpClient.PostAsJsonAsync("odata/Tags", tag);
            if (response.IsSuccessStatusCode) return ServiceResult<bool>.Ok(true);
            return ServiceResult<bool>.Fail("Failed to add tag.");
        }
    }
}
