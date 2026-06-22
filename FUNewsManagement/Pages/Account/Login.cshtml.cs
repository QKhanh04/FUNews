using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using FUNewsManagement.Services;
using System.Security.Claims;
using ViewModel.Account;

namespace FUNewsManagement.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly IAccountService _accountService;

        public LoginModel(IAccountService accountService)
        {
            _accountService = accountService;
        }

        [BindProperty]
        public LoginViewModel Input { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? ReturnUrl { get; set; }

        public string? ErrorMessage { get; set; }

        public IActionResult OnGet(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToPage("/Index");

            ReturnUrl = returnUrl;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            ViewData["CurrentEmail"] = Input.Email;
            var result = await _accountService.Login(Input.Email, Input.Password);

            if (!result.IsSuccess)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                return Page();
            }

            var account = result.Data!;
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, account.AccountId.ToString()),
                new Claim(ClaimTypes.Name, account.AccountName ?? string.Empty),
                new Claim(ClaimTypes.Email, account.AccountEmail ?? string.Empty),
                new Claim(ClaimTypes.Role, account.AccountRole?.ToString() ?? "3"),
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            var authProps = new AuthenticationProperties
            {
                IsPersistent = Input.RememberMe
            };

            if (Input.RememberMe)
            {
                authProps.ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30);
            }

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                authProps);

            if (!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
                return Redirect(ReturnUrl);

            TempData["ToastMessage"] = result.Message;
            TempData["ToastType"] = "success";

            return RedirectToPage("/Index");
        }

        public IActionResult OnGetLoginWithGoogle(string? returnUrl = null)
        {
            var redirectUrl = Url.Page("/Account/Login", "GoogleResponse", new { returnUrl });
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        public async Task<IActionResult> OnGetGoogleResponseAsync(string? returnUrl = null)
        {
            var info = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);
            if (info?.Principal == null)
            {
                ErrorMessage = "Google login failed. Please try again.";
                return Page();
            }

            var googleId = info.Principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var email = info.Principal.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
            var name = info.Principal.FindFirstValue(ClaimTypes.Name) ?? string.Empty;

            var result = await _accountService.LoginWithGoogleAsync(googleId, email, name);
            if (!result.IsSuccess)
            {
                ErrorMessage = result.Message;
                return Page();
            }

            var account = result.Data!;
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, account.AccountId.ToString()),
                new Claim(ClaimTypes.Name,           account.AccountName ?? string.Empty),
                new Claim(ClaimTypes.Email,          account.AccountEmail ?? string.Empty),
                new Claim(ClaimTypes.Role,           account.AccountRole?.ToString() ?? "3"),
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
                new AuthenticationProperties { IsPersistent = true });

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            TempData["ToastMessage"] = result.Message;
            TempData["ToastType"] = "success";
            return RedirectToPage("/Index");
        }
    }
}
