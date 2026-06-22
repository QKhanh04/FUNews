using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Service.Interface;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ViewModel.Account;

namespace FUNewsManagement_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAccountService _accountService;
        private readonly IConfiguration _configuration;

        public AuthController(IAccountService accountService, IConfiguration configuration)
        {
            _accountService = accountService;
            _configuration = configuration;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var result = await _accountService.Login(request.Email, request.Password);
            
            if (!result.IsSuccess || result.Data == null)
            {
                return Unauthorized(new { message = result.Message });
            }

            var account = result.Data;

            var authClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, account.AccountId.ToString()),
                new Claim(ClaimTypes.Email, account.AccountEmail ?? ""),
                new Claim(ClaimTypes.Name, account.AccountName ?? ""),
                new Claim(ClaimTypes.Role, account.AccountRole.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var jwtKey = _configuration["Jwt:Key"];
            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey!));

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                expires: DateTime.Now.AddHours(3),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
            );

            return Ok(new
            {
                token = new JwtSecurityTokenHandler().WriteToken(token),
                expiration = token.ValidTo,
                user = new {
                    account.AccountId,
                    account.AccountName,
                    account.AccountEmail,
                    account.AccountRole
                }
            });
        }

        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
        {
            var result = await _accountService.LoginWithGoogleAsync(request.GoogleId, request.Email, request.Name);
            
            if (!result.IsSuccess || result.Data == null)
            {
                return Unauthorized(new { message = result.Message });
            }

            var account = result.Data;

            var authClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, account.AccountId.ToString()),
                new Claim(ClaimTypes.Email, account.AccountEmail ?? ""),
                new Claim(ClaimTypes.Name, account.AccountName ?? ""),
                new Claim(ClaimTypes.Role, account.AccountRole.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var jwtKey = _configuration["Jwt:Key"];
            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey!));

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                expires: DateTime.Now.AddHours(3),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
            );

            return Ok(new
            {
                token = new JwtSecurityTokenHandler().WriteToken(token),
                expiration = token.ValidTo,
                user = new {
                    account.AccountId,
                    account.AccountName,
                    account.AccountEmail,
                    account.AccountRole
                }
            });
        }
    }

    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class GoogleLoginRequest
    {
        public string GoogleId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}
