using Common;
using DataAccessObjects;
using ViewModel.Account;
using DataAccessObjects;
using FUNewsManagement.Services;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ViewModel.Account;

namespace FUNewsManagement.Services
{
    public class AccountApiClient : IAccountService
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AccountApiClient(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<ServiceResult<SystemAccount>> Login(string email, string password)
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/login", new { email, password });
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
                if (result != null)
                {
                    // Attach token to HttpContext cookies directly or we just return the account and let PageModel do it.
                    // Wait, PageModel doesn't know about JWT, it uses CookieAuth. 
                    // To keep PageModel working as-is, we can store JWT in a cookie during login,
                    // and return a SystemAccount object to satisfy the existing code!
                    
                    if (_httpContextAccessor.HttpContext != null)
                    {
                        _httpContextAccessor.HttpContext.Response.Cookies.Append("jwtToken", result.Token, new CookieOptions
                        {
                            HttpOnly = true,
                            Secure = true,
                            SameSite = SameSiteMode.Strict,
                            Expires = result.Expiration
                        });
                    }

                    return ServiceResult<SystemAccount>.Ok(new SystemAccount
                    {
                        AccountId = result.User.AccountId,
                        AccountEmail = result.User.AccountEmail,
                        AccountName = result.User.AccountName,
                        AccountRole = result.User.AccountRole
                    });
                }
            }
            var err = await response.Content.ReadAsStringAsync();
            return ServiceResult<SystemAccount>.Fail(err);
        }

        public async Task<ServiceResult<SystemAccount>> LoginWithGoogleAsync(string googleId, string email, string name)
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/google-login", new { GoogleId = googleId, Email = email, Name = name });
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
                if (result?.Token != null)
                {
                    // Trả về SystemAccount để UI tiếp tục logic lưu vào HTTP Context Cookie (tương thích ngược với UI cũ)
                    var account = new SystemAccount
                    {
                        AccountId = result.User.AccountId,
                        AccountEmail = result.User.AccountEmail,
                        AccountName = result.User.AccountName,
                        AccountRole = result.User.AccountRole,
                        // Lưu token vào một trường tạm để UI có thể lấy ra lưu vào cookies.
                        // (SystemAccount không có trường Token, ta mượn trường AccountPassword hoặc thiết lập ở UI)
                    };
                    
                    // We must pass the token to the frontend somehow.
                    // The easiest way is to let the frontend Login.cshtml.cs handle it just like it handles standard login.
                    // The standard login method in AccountApiClient attached the token to the HTTP context directly!
                    // Let's do that here too.
                    
                    var cookieOptions = new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.Strict,
                        Expires = result.Expiration
                    };
                    _httpContextAccessor.HttpContext?.Response.Cookies.Append("jwtToken", result.Token, cookieOptions);

                    return ServiceResult<SystemAccount>.Ok(account);
                }
            }

            var error = await response.Content.ReadAsStringAsync();
            return ServiceResult<SystemAccount>.Fail(error);
        }

        public async Task<ServiceResult<SystemAccount>> GetAccountById(short id)
        {

            var response = await _httpClient.GetAsync($"odata/Accounts({id})");
            if (response.IsSuccessStatusCode)
            {
                var account = await response.Content.ReadFromJsonAsync<SystemAccount>();
                return ServiceResult<SystemAccount>.Ok(account!);
            }
            return ServiceResult<SystemAccount>.Fail("Failed to fetch");
        }

        public async Task<ServiceResult<bool>> UpdateAccount(SystemAccount account, short currentUserId)
        {

            var response = await _httpClient.PutAsJsonAsync($"odata/Accounts/{account.AccountId}", account);
            return response.IsSuccessStatusCode 
                ? ServiceResult<bool>.Ok(true) 
                : ServiceResult<bool>.Fail("Update failed");
        }

        public async Task<ServiceResult<bool>> ChangePassword(short accountId, string oldPassword, string newPassword)
        {
            throw new NotImplementedException();
        }

        public async Task<AccountManagementViewModel> GetAccountManagementAsync(string? search, int pageNumber, int pageSize)
        {

            var response = await _httpClient.GetAsync($"odata/Accounts?$count=true&$top={pageSize}&$skip={(pageNumber - 1) * pageSize}");
            if (response.IsSuccessStatusCode)
            {
                // Simple placeholder, real implementation needs OData result parsing
                return new AccountManagementViewModel { Accounts = new List<AccountItemViewModel>() };
            }
            return new AccountManagementViewModel();
        }

        public async Task<ServiceResult<bool>> AddAccount(SystemAccount account)
        {

            var response = await _httpClient.PostAsJsonAsync("odata/Accounts", account);
            return response.IsSuccessStatusCode 
                ? ServiceResult<bool>.Ok(true) 
                : ServiceResult<bool>.Fail("Add failed");
        }

        public async Task<ServiceResult<bool>> DeleteAccount(short id)
        {

            var response = await _httpClient.DeleteAsync($"odata/Accounts/{id}");
            return response.IsSuccessStatusCode 
                ? ServiceResult<bool>.Ok(true) 
                : ServiceResult<bool>.Fail("Delete failed");
        }

        private class LoginResponse
        {
            public string Token { get; set; } = string.Empty;
            public DateTime Expiration { get; set; }
            public LoginUser User { get; set; } = new();
        }

        private class LoginUser
        {
            public short AccountId { get; set; }
            public string? AccountName { get; set; }
            public string? AccountEmail { get; set; }
            public int? AccountRole { get; set; }
        }
    }

    public class AuthResponse
    {
        public string Token { get; set; } = string.Empty;
        public DateTime Expiration { get; set; }
        public AuthUserResponse User { get; set; } = new();
    }

    public class AuthUserResponse
    {
        public short AccountId { get; set; }
        public string? AccountName { get; set; }
        public string? AccountEmail { get; set; }
        public int? AccountRole { get; set; }
    }
}
