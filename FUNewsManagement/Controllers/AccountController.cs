using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Service.Interface;
using Microsoft.AspNetCore.Authentication.Google;
using ViewModel.Account;
using Newtonsoft.Json.Linq;
using DataAccessObjects;

namespace FUNewsManagement.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAccountService _accountService;

        public AccountController(IAccountService accountService)
        {
            _accountService = accountService;
        }
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            var model = new LoginViewModel
            {
                ReturnUrl = returnUrl
            };

            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);
            ViewData["CurrentEmail"] = model.Email;
            var result = await _accountService.Login(model.Email, model.Password);

            if (!result.IsSuccess)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                return View(model);
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
                IsPersistent = model.RememberMe
            };

            if (model.RememberMe)
            {
                authProps.ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30);
            }

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                authProps);

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                return Redirect(model.ReturnUrl);

            TempData["ToastMessage"] = result.Message;
            TempData["ToastType"] = "success";

            return RedirectToAction("Index", "Home");
        }
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            TempData["ToastMessage"] = "Logout successfully!!";
            TempData["ToastType"] = "success";
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult LoginWithGoogle(string? returnUrl = null)
        {
            var redirectUrl = Url.Action("GoogleResponse", "Account", new { returnUrl });
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet]
        public async Task<IActionResult> GoogleResponse(string? returnUrl = null)
        {
            var info = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);
            if (info?.Principal == null)
            {
                ViewData["Error"] = "Google login failed. Please try again.";
                return View("Login");
            }

            var googleId = info.Principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var email = info.Principal.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
            var name = info.Principal.FindFirstValue(ClaimTypes.Name) ?? string.Empty;

            var result = await _accountService.LoginWithGoogleAsync(googleId, email, name);
            if (!result.IsSuccess)
            {
                ViewData["Error"] = result.Message;
                return View("Login");
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
            return RedirectToAction("Index", "Home");
        }

        public IActionResult AccessDenied() => View();

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!short.TryParse(userIdStr, out short userId))
                return RedirectToAction("Login");

            var result = await _accountService.GetAccountById(userId);
            if (!result.IsSuccess)
            {
                TempData["ToastMessage"] = result.Message;
                TempData["ToastType"] = "danger";
                return RedirectToAction("Index", "Home");
            }

            var account = result.Data!;
            var model = new ProfileViewModel
            {
                AccountId    = account.AccountId,
                AccountName  = account.AccountName,
                AccountEmail = account.AccountEmail,
                AccountRole  = account.AccountRole,
                IsGoogleAccount = account.IsGoogleAccount ?? false,
                GoogleId     = account.GoogleId,
                CreatedAt    = account.CreatedAt
                
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(ProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await RePopulateProfileModel(model);
                return View("Profile", model);
            }

            var account = new SystemAccount
            {
                AccountId = model.AccountId,
                AccountName = model.AccountName
                // Add other fields if necessary
            };

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            short.TryParse(userIdStr, out short currentUserId);

            var result = await _accountService.UpdateAccount(account, currentUserId);
            if (!result.IsSuccess)
            {
                TempData["ToastMessage"] = result.Message;
                TempData["ToastType"] = "danger";
                await RePopulateProfileModel(model);
                return View("Profile", model);
            }

            TempData["ToastMessage"] = result.Message;
            TempData["ToastType"] = "success";

            return RedirectToAction("Profile");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ProfileViewModel model)
        {
            if (string.IsNullOrEmpty(model.CurrentPassword) || string.IsNullOrEmpty(model.NewPassword) || string.IsNullOrEmpty(model.ConfirmPassword))
            {
                TempData["ToastMessage"] = "Please fill in all password fields.";
                TempData["ToastType"] = "warning";
                await RePopulateProfileModel(model);
                return View("Profile", model);
            }

            if (model.NewPassword != model.ConfirmPassword)
            {
                ModelState.AddModelError("ConfirmPassword", "New password and confirmation do not match.");
                await RePopulateProfileModel(model);
                return View("Profile", model);
            }

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!short.TryParse(userIdStr, out short userId))
                return RedirectToAction("Login");

            var result = await _accountService.ChangePassword(userId, model.CurrentPassword, model.NewPassword);
            if (!result.IsSuccess)
            {
                TempData["ToastMessage"] = result.Message;
                TempData["ToastType"] = "danger";
                await RePopulateProfileModel(model);
                return View("Profile", model);
            }

            TempData["ToastMessage"] = result.Message;
            TempData["ToastType"] = "success";

            return RedirectToAction("Profile");
        }

        private async Task RePopulateProfileModel(ProfileViewModel model)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (short.TryParse(userIdStr, out short userId))
            {
                var result = await _accountService.GetAccountById(userId);
                if (result.IsSuccess)
                {
                    var account = result.Data!;
                    model.AccountId = account.AccountId;
                    model.AccountName = account.AccountName;
                    model.AccountEmail = account.AccountEmail;
                    model.AccountRole = account.AccountRole;
                    model.IsGoogleAccount = account.IsGoogleAccount ?? false;
                    model.GoogleId = account.GoogleId;
                    model.CreatedAt = account.CreatedAt;

                    // Clear validation error for AccountName because it's re-fetched from DB 
                    // and not expected to be in the ChangePassword form
                    ModelState.Remove("AccountName");
                }
            }
        }

        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "0")]
        [HttpGet]
        public async Task<IActionResult> Manage(string? search, int pageNumber = 1, int pageSize = 10)
        {
            var result = await _accountService.GetAccountManagementAsync(search, pageNumber, pageSize);
            ViewBag.CurrentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return View(result);
        }

        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "0")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SystemAccount account)
        {
            var result = await _accountService.AddAccount(account);
            if (result.IsSuccess)
            {
                TempData["ToastMessage"] = result.Message;
                TempData["ToastType"] = "success";
            }
            else
            {
                TempData["ToastMessage"] = result.Message;
                TempData["ToastType"] = "danger";
            }
            return RedirectToAction("Manage");
        }

        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "0")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(SystemAccount account)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            short.TryParse(userIdStr, out short currentUserId);

            var result = await _accountService.UpdateAccount(account, currentUserId);
            if (result.IsSuccess)
            {
                TempData["ToastMessage"] = result.Message;
                TempData["ToastType"] = "success";
            }
            else
            {
                TempData["ToastMessage"] = result.Message;
                TempData["ToastType"] = "danger";
            }
            return RedirectToAction("Manage");
        }

        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "0")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(short id)
        {
            var result = await _accountService.DeleteAccount(id);
            if (result.IsSuccess)
            {
                TempData["ToastMessage"] = result.Message;
                TempData["ToastType"] = "success";
            }
            else
            {
                TempData["ToastMessage"] = result.Message;
                TempData["ToastType"] = "danger";
            }
            return RedirectToAction("Manage");
        }
    }
}
